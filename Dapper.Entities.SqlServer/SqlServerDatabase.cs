using Dapper.Entities.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Dapper.Entities.SqlServer;

public class SqlServerDatabase(string connectionString, ILogger<SqlServerDatabase> logger, ISqlBuilder sqlBuilder) : Database(logger, sqlBuilder)
{
	private readonly string _connectionString = connectionString;

	public SqlServerDatabase(string connectionString, ILogger<SqlServerDatabase> logger) : this(connectionString, logger, new DefaultSqlBuilder())
	{
	}

	public override IDbConnection GetConnection() => new SqlConnection(_connectionString);
}