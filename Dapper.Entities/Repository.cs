using Dapper.Entities.Exceptions;
using Dapper.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Dapper.Entities;

public enum RepositoryAction
{
	Insert,
	Update,
	Delete,
	Get
}

public class Repository<TDatabase, TEntity, TKey>
	where TDatabase : Database
	where TEntity : IEntity<TKey>
	where TKey : struct
{
	private readonly SqlStatements _sqlStatements;

	protected readonly TDatabase Database;

	public Repository(TDatabase database)
	{
		Database = database;
		_sqlStatements = Database.SqlBuilder.BuildStatements(typeof(TEntity));
	}

	protected virtual CommandType GetCommandType => CommandType.Text;
	protected virtual CommandType InsertCommandType => CommandType.Text;
	protected virtual CommandType UpdateCommandType => CommandType.Text;
	protected virtual CommandType DeleteCommandType => CommandType.Text;

	protected virtual string? CustomGetCommand { get; }
	protected virtual string? CustomInsertCommand { get; }
	protected virtual string? CustomUpdateCommand { get; }
	protected virtual string? CustomDeleteCommand { get; }

	public async Task<TEntity?> GetAsync(IDbConnection connection, TKey id, IDbTransaction? transaction = null)
	{
		var sql = CustomGetCommand ?? _sqlStatements.GetById;
		return await GetInnerAsync(connection, sql, new { id }, transaction);
	}

	public async Task<TEntity?> GetAsync(TKey id)
	{
		using var cn = Database.GetConnection();
		return await GetAsync(cn, id);
	}

	public async Task<TEntity?> GetAlternateAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction = null)
	{
        if (!_sqlStatements.HasAlternateKey)
        {
            throw new NotImplementedException($"Entity type {typeof(TEntity).Name} must have at least one updateable column with the [Key] attribute to use with the MergeAsync method");
        }

        return await GetInnerAsync(connection, _sqlStatements.GetByAlternateKey, entity, transaction, "GetAlternate");
    }

	public async Task<TEntity?> GetAlternateAsync(TEntity entity)
	{
		using var cn = Database.GetConnection();
		return await GetAlternateAsync(cn, entity);
	}

	public async Task<TEntity> MustGetAsync(IDbConnection connection, TKey id, IDbTransaction? transaction = null) =>
		await GetAsync(connection, id, transaction) ?? throw new Exception($"{typeof(TEntity).Name} row id {id} not found");

	public async Task<TEntity> MustGetAsync(TKey id) => 
		await GetAsync(id) ?? throw new Exception($"{typeof(TEntity).Name} row id {id} not found");

	protected async Task<TEntity?> GetInnerAsync(IDbConnection connection, string sql, object parameter, IDbTransaction? transaction = null, string action = "Get")
	{
		Database.Logger.LogTrace("{action}: {query}, Id = {id}", action, sql, parameter);

		TEntity? result;

		try
		{
			result = await connection.QuerySingleOrDefaultAsync<TEntity>(sql, parameter, transaction, commandType: GetCommandType);
		}
		catch (Exception exc)
		{
			Database.Logger.LogError(exc, "Error executing query: {sql} with params {@parameter}", sql, parameter);
			throw new RepositoryException(RepositoryAction.Get, exc.Message);
		}

		if (result is not null)
		{
			var (allow, message) = await AllowGetAsync(connection, result, transaction);
			if (!allow) throw new RepositoryException(RepositoryAction.Get, message!);

			await AfterGetAsync(connection, result, transaction);
		}

		return result;
	}

	public async Task<TEntity> SaveAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction = null)
	{
		ArgumentNullException.ThrowIfNull(nameof(entity));

		var action = entity.IsNew() ? RepositoryAction.Insert : RepositoryAction.Update;

		var (allow, message) = await AllowSaveAsync(connection, action, entity, transaction);
		if (!allow) throw new RepositoryException(action, message!);

		var sql = (action is RepositoryAction.Insert) ?
			CustomInsertCommand ?? _sqlStatements.Insert :
			CustomUpdateCommand ?? _sqlStatements.Update;

		var commandType = (action is RepositoryAction.Insert) ? InsertCommandType : UpdateCommandType;

		await BeforeSaveAsync(connection, action, entity, transaction);

		Database.Logger.LogTrace("{entityType}.{action}: {query}, {@entity}", typeof(TEntity).Name, action, sql, entity);

		try
		{
			var id = await connection.ExecuteScalarAsync<TKey>(sql, entity, transaction, commandType: commandType);
			if (action is RepositoryAction.Insert) entity.Id = id;
		}
		catch (Exception exc)
		{
			Database.Logger.LogError(exc, "Error executing query: {sql} with params {@entity}", sql, entity);
			throw new RepositoryException(action, exc.Message, exc) { Sql = sql, Parameters = entity };
		}

		await AfterSaveAsync(connection, action, entity, transaction);

		return entity;
	}

	public async Task<TEntity> SaveAsync(TEntity entity)
	{
		using var cn = Database.GetConnection();		
		return await SaveAsync(cn, entity);
	}

	public async Task<TEntity> MergeAsync(IDbConnection connection, TEntity entity, Action<TEntity, TEntity>? onExisting = null, IDbTransaction? transaction = null)
	{
		if (entity.IsNew())
		{
			var existing = await GetAlternateAsync(connection, entity, transaction);
			if (existing is not null)
			{
				onExisting?.Invoke(entity, existing);
				entity.Id = existing.Id;
			}
		}

		return await SaveAsync(entity);
	}

	public async Task<TEntity> MergeAsync(TEntity entity, Action<TEntity, TEntity>? onExisting = null)
	{
		using var cn = Database.GetConnection();
		return await MergeAsync(cn, entity, onExisting);
	}

	public async Task DeleteAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction = null)
	{
		ArgumentNullException.ThrowIfNull(nameof(entity));

		var (allow, message) = await AllowDeleteAsync(connection, entity, transaction);
		if (!allow) throw new RepositoryException(RepositoryAction.Delete, message!);

		await BeforeDeleteAsync(connection, entity, transaction);

		var sql = CustomDeleteCommand ?? _sqlStatements.Delete;
		Database.Logger.LogTrace("{entityType}.{action}: {query}, {@entity}", typeof(TEntity).Name, RepositoryAction.Delete, sql, entity);

		var param = new { id = entity.Id };

		try
		{
			await connection.ExecuteAsync(sql, param, transaction, commandType: DeleteCommandType);
		}
		catch (Exception exc)
		{
			Database.Logger.LogError(exc, "Error executing query: {sql} with params {@param}", sql, param);
			throw new RepositoryException(RepositoryAction.Delete, exc.Message, exc) { Sql = sql, Parameters = param };
		}

		await AfterDeleteAsync(connection, entity, transaction);
	}

	public async Task DeleteAsync(TEntity entity)
	{
		using var cn = Database.GetConnection();
		await DeleteAsync(cn, entity);
	}

	public async Task UpdateAsync(TKey id, Action<TEntity> setProperties, Func<TEntity>? ifNotExists = null)
	{
		using var cn = Database.GetConnection();
		await UpdateAsync(cn, id, setProperties, ifNotExists);
	}

	public async Task UpdateAsync(IDbConnection connection, TKey id, Action<TEntity> setProperties, Func<TEntity>? ifNotExists = null, IDbTransaction? transaction = null)
	{
		TEntity? row = await GetAsync(connection, id, transaction);

		if (row is null && ifNotExists is not null)
		{
			row = ifNotExists.Invoke();
		}

		ArgumentNullException.ThrowIfNull(row, nameof(row));

		setProperties(row);

		await SaveAsync(connection, row, transaction);
	}

	protected virtual async Task AfterGetAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction) => await Task.CompletedTask;

	protected virtual async Task<(bool Result, string? Message)> AllowGetAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction) => await Task.FromResult((true, default(string)));

	protected virtual async Task<(bool Result, string? Message)> AllowSaveAsync(IDbConnection connection, RepositoryAction action, TEntity entity, IDbTransaction? transaction) => await Task.FromResult((true, default(string)));

	protected virtual async Task BeforeSaveAsync(IDbConnection connection, RepositoryAction action, TEntity entity, IDbTransaction? transaction) => await Task.CompletedTask;

	protected virtual async Task AfterSaveAsync(IDbConnection connection, RepositoryAction action, TEntity entity, IDbTransaction? transaction) => await Task.CompletedTask;

	protected virtual async Task<(bool Result, string? Message)> AllowDeleteAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction) => await Task.FromResult((true, default(string)));

	protected virtual async Task BeforeDeleteAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction) => await Task.CompletedTask;

	protected virtual async Task AfterDeleteAsync(IDbConnection connection, TEntity entity, IDbTransaction? transaction) => await Task.CompletedTask;
}
