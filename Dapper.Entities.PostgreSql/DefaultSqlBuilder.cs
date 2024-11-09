using Dapper.Entities.Abstract;
using Dapper.Entities.Abstractions.Models;
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
		var (schema, name) = ParseTableName(entityType, "public", FormatName);
		var tableName = $"{schema}.{name}";
		var columnMappings = GetColumnMappings(entityType);
		bool hasAlternateKey = HasAlternateKey(columnMappings);
		var columnsByPropertyName = columnMappings.ToDictionary(col => col.ParameterName);
		var identityCol = columnsByPropertyName["Id"].ColumnName;
		var selectColumnNames = string.Join(", ", columnMappings.Select(col => $"{FormatName(col.ColumnName)} AS {col.ParameterName}"));

		return new()
		{
			GetById = $"SELECT {selectColumnNames} FROM {tableName} WHERE {FormatName(identityCol)} = @id",
			HasAlternateKey = hasAlternateKey,
			GetByAlternateKey = hasAlternateKey ? BuildGetByAlternateKey(tableName, selectColumnNames, columnMappings) : string.Empty,
			Insert = BuildInsert(tableName, columnMappings),
			Update = BuildUpdate(tableName, columnMappings),
			Delete = BuildDelete(tableName, columnMappings),
			ColumnMappings = columnMappings,
			TableName = tableName,			
			UpdateColumns = (properties) => BuildUpdate(tableName, columnMappings, properties)
		};
	}

	private string BuildDelete(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var deleteCriteria = GetDeleteCriteria(columns, SetExpression);
		return $"DELETE FROM {tableName} WHERE {deleteCriteria}";
	}

	private string BuildUpdate(string tableName, IEnumerable<ColumnMapping> columns, IEnumerable<string>? updateColumns = null)
	{
		var (setColumns, whereClause) = GetUpdateComponents(columns, SetExpression);
		return $"UPDATE {tableName} SET {setColumns} WHERE {whereClause}";
	}

	private string BuildGetByAlternateKey(string tableName, string columnNames, IEnumerable<ColumnMapping> columns)
	{
		var criteria = GetKeyColumnCriteria(tableName, columns, SetExpression);
		return $"SELECT {columnNames} FROM {tableName} WHERE {criteria}";
	}

	private string BuildInsert(string tableName, IEnumerable<ColumnMapping> columns, IEnumerable<string>? propertyNames = null)
	{
		var (names, values) = GetInsertComponents(columns, (col) => FormatName(col.ColumnName), propertyNames);
		return $@"INSERT INTO {tableName} ({names}) VALUES ({values}) RETURNING {FormatName("Id")};";
	}

	private string FormatName(string input) => CaseConversion switch
	{
		CaseConversionOptions.Exact => $"\"{input}\"",
		CaseConversionOptions.SnakeCase => ToSnakeCase(input),
		_ => input
	};

	public static string ToSnakeCase(string input) =>
		string.Join(string.Empty, input.Select((c, index) => (char.IsUpper(c) && index > 0) ? $"_{c.ToString().ToLower()}" : c.ToString().ToLower()));

	private string SetExpression(ColumnMapping columnMapping) => $"{FormatName(columnMapping.ColumnName)}=@{columnMapping.ParameterName}";
}
