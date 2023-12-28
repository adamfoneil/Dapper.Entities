using Dapper.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Dapper.Entities.PostgreSql;

public class PostgreSqlDatabase : Database
{
	private readonly string _connectionString;

	public PostgreSqlDatabase(string connectionString, ILogger<PostgreSqlDatabase> logger) : this(connectionString, logger, new DefaultSqlBuilder())
	{
	}

	public PostgreSqlDatabase(string connectionString, ILogger<PostgreSqlDatabase> logger, ISqlBuilder sqlBuilder) : base(logger, sqlBuilder)
	{
		_connectionString = connectionString;
	}

	public override IDbConnection GetConnection() => new NpgsqlConnection(_connectionString);
	
}
