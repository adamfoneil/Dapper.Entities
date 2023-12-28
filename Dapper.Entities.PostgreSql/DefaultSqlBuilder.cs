using Dapper.Entities.Abstract;
using Dapper.Entities.Interfaces;
using System.Data;

namespace Dapper.Entities.PostgreSql;

public enum CaseConversionOptions
{
	/// <summary>
	/// use original name
	/// </summary>
	None,
	/// <summary>
	/// lower case with underscores before each capital
	/// </summary>
	SnakeCase,
	/// <summary>
	/// enclose names in double quotes
	/// </summary>
	Exact
}

public class DefaultSqlBuilder : SqlBuilder
{
	public CaseConversionOptions CaseConversion { get; init; }

	public override SqlStatements BuildStatements(Type entityType)
	{
		var (schema, name) = ParseTableName(entityType, "public");
		var tableName = $"{schema}.{name}";

		return new()
		{
			GetById = $"SELECT * FROM {tableName} WHERE id = @id",
			GetByAlternateKey = BuildGetByAlternateKey(tableName, entityType),
			Insert = BuildInsert(tableName, entityType),
			Update = BuildUpdate(tableName, entityType),
			Delete = BuildDelete(tableName, entityType)
		};
	}

	private string BuildDelete(string tableName, Type entityType)
	{
		throw new NotImplementedException();
	}

	private string BuildUpdate(string tableName, Type entityType)
	{
		throw new NotImplementedException();
	}

	private string BuildGetByAlternateKey(string tableName, Type entityType)
	{
		throw new NotImplementedException();
	}

	private string FormatName(string input) => CaseConversion switch
	{
		CaseConversionOptions.Exact => $"\"{input}\"",
		CaseConversionOptions.SnakeCase => ToSnakeCase(input),
		_  => input
	};

	private static string ToSnakeCase(string input) =>
		string.Join(string.Empty, input.Select((c, index) => (char.IsUpper(c) && index > 0) ? $"_{c}" : c.ToString()));

	private string BuildInsert(string tableName, Type entityType)
	{
		var columns = GetColumnMappings(entityType, StatementType.Insert).Where(col => col.ForInsert).ToArray();
		if (!columns.Any(col => col.ForInsert)) throw new ArgumentException("Must have at least one insert column");

		var insertCols = string.Join(", ", columns.Select(col => FormatName(col.ColumnName)));
		var insertValues = string.Join(", ", columns.Select(col => $"@{col.ParameterName}"));

		return
			$@"INSERT INTO {tableName} (
                {insertCols}
            ) VALUES (
                {insertValues}
            ) RETURNING {FormatName("id")};";
	}
}
