using Dapper.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Dapper.Entities;

public abstract class Database(ILogger<Database> logger, ISqlBuilder sqlBuilder)
{
	public readonly ILogger<Database> Logger = logger;
	public readonly ISqlBuilder SqlBuilder = sqlBuilder;

	public abstract IDbConnection GetConnection();

	/// <summary>
	/// general-purpose unit-of-work wrapper
	/// </summary>
	public async Task DoTransactionAsync(Func<IDbConnection, IDbTransaction, Task> work)
	{
		using var cn = GetConnection();
		if (cn.State != ConnectionState.Open) cn.Open();

		using var txn = cn.BeginTransaction();
		try
		{
			await work.Invoke(cn, txn);
			txn.Commit();
		}
		catch
		{
			txn.Rollback();
			throw;
		}
	}

	public async Task<TResult> DoTransactionAsync<TResult>(Func<IDbConnection, IDbTransaction, Task<TResult>> work)
    {
        using var cn = GetConnection();
        if (cn.State != ConnectionState.Open) cn.Open();

        using var txn = cn.BeginTransaction();
        try
        {
            var result = await work.Invoke(cn, txn);
            txn.Commit();
			return result;
        }
        catch
        {
            txn.Rollback();
            throw;
        }
    }
}