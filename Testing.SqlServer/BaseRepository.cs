using Dapper.Entities;
using Dapper.Entities.Interfaces;
using System.Data;
using Testing.Data.Conventions;

namespace Testing.SqlServer;

public class BaseRepository<TEntity>(LiteInvoiceDatabase database) : Repository<LiteInvoiceDatabase, TEntity, long>(database) where TEntity : IEntity<long>
{
	protected override async Task BeforeSaveAsync(IDbConnection connection, RepositoryAction action, TEntity entity, IDbTransaction? transaction)
	{
		if (entity is BaseTable table)
		{
			if (action is RepositoryAction.Insert) table.DateCreated = DateTime.Now;
			if (action is RepositoryAction.Update) table.DateModified = DateTime.Now;
		}

		await Task.CompletedTask;
	}
}
