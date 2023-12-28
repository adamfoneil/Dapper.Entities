using Dapper.Entities.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Dapper.Entities.SqlServer;

public class SqlServerDatabase : Database
{
	private readonly string _connectionString;

	public SqlServerDatabase(string connectionString, ILogger<SqlServerDatabase> logger) : this(connectionString, logger, new DefaultSqlBuilder())
	{
	}

	public SqlServerDatabase(string connectionString, ILogger<SqlServerDatabase> logger, ISqlBuilder sqlBuilder) : base(logger, sqlBuilder)
	{
		_connectionString = connectionString;
	}

	public override IDbConnection GetConnection() => new SqlConnection(_connectionString);
}