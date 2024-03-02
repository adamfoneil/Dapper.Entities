using Dapper.Entities.Interfaces;
using System.Data;

namespace Dapper.Entities.Extensions;

public static class CrudExtensions
{
	/// <summary>
	/// global SQL builder convention -- set this in startup before using any of the crud extension methods
	/// </summary>
	public static ISqlBuilder? SqlBuilder { get; set; }

	private static readonly Dictionary<Type, SqlStatements> TypeSqlBuilders = [];

	public static async Task<TEntity?> GetAsync<TEntity, TKey>(this IDbConnection connection, TKey id, IDbTransaction? transaction = null)
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		var sql = GetSqlStatements<TEntity, TKey>().GetById;
		return await connection.QuerySingleOrDefaultAsync<TEntity>(sql, new { id }, transaction);
	}

	public static async Task<TEntity?> GetByAlternateKeyAsync<TEntity, TKey>(this IDbConnection connection, TEntity entity, IDbTransaction? transaction = null)
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		var statements = GetSqlStatements<TEntity, TKey>();
		if (!statements.HasAlternateKey) throw new NotImplementedException($"Entity class {typeof(TEntity).Name} is missing one or more [Key] properties.");
		var sql = statements.GetByAlternateKey;
		return await connection.QuerySingleOrDefaultAsync<TEntity>(sql, entity, transaction);
	}

	public static async Task<TEntity> SaveAsync<TEntity, TKey>(this IDbConnection connection, TEntity entity, IDbTransaction? transaction = null)
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		if (entity.IsNew())
		{
			return await InsertAsync<TEntity, TKey>(connection, entity, transaction);
		}
		else
		{
			await UpdateAsync<TEntity, TKey>(connection, entity, transaction);
			return entity;
		}
	}

	/// <summary>
	/// when saving a new row, checks for an existing row based on a combination of [Key] properties before inserting
	/// </summary>
	public static async Task<TEntity> MergeAsync<TEntity, TKey>(this IDbConnection connection, TEntity entity, IDbTransaction? transaction = null, Action<TEntity, TEntity>? onExisting = null)
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		if (entity.IsNew())
		{
			var existing = await GetByAlternateKeyAsync<TEntity, TKey>(connection, entity, transaction);
			if (existing != null)
			{
				onExisting?.Invoke(entity, existing);
				entity.Id = existing.Id;
			}
		}

		return await SaveAsync<TEntity, TKey>(connection, entity, transaction);
	}

	public static async Task<TEntity> InsertAsync<TEntity, TKey>(this IDbConnection connection, TEntity entity, IDbTransaction? transaction = null)
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		var sql = GetSqlStatements<TEntity, TKey>().Insert;
		var id = await connection.ExecuteScalarAsync<TKey>(sql, entity, transaction);
		entity.Id = id;

		return entity;
	}

	public static async Task UpdateAsync<TEntity, TKey>(this IDbConnection connection, TEntity entity, IDbTransaction? transaction = null)
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		var sql = GetSqlStatements<TEntity, TKey>().Update;
		await connection.ExecuteAsync(sql, entity, transaction);
	}

	public static async Task DeleteAsync<TEntity, TKey>(this IDbConnection connection, TKey id, IDbTransaction? transaction = null)
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		var sql = GetSqlStatements<TEntity, TKey>().Delete;
		await connection.ExecuteAsync(sql, new { id }, transaction);
	}

	private static SqlStatements GetSqlStatements<TEntity, TKey>()
		where TEntity : IEntity<TKey>
		where TKey : struct
	{
		ArgumentNullException.ThrowIfNull(SqlBuilder, nameof(SqlBuilder));

		if (!TypeSqlBuilders.ContainsKey(typeof(TEntity)))
		{
			TypeSqlBuilders.Add(typeof(TEntity), SqlBuilder.BuildStatements(typeof(TEntity)));
		}

		return TypeSqlBuilders[typeof(TEntity)];
	}
}
