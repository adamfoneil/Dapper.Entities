using Dapper.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Dapper.Entities.PostgreSql;

public class PostgreSqlDatabase(string connectionString, ILogger<PostgreSqlDatabase> logger, ISqlBuilder sqlBuilder) : Database(logger, sqlBuilder)
{
	private readonly string _connectionString = connectionString;

	public PostgreSqlDatabase(string connectionString, ILogger<PostgreSqlDatabase> logger) : this(connectionString, logger, new DefaultSqlBuilder())
	{
	}

	public override IDbConnection GetConnection() => new NpgsqlConnection(_connectionString);	
}
