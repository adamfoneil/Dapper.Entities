using Dapper.Entities.Abstract;
using Dapper.Entities.Abstractions.Models;
using Dapper.Entities.Interfaces;

namespace Dapper.Entities.SqlServer;

public class DefaultSqlBuilder : SqlBuilder
{
	public override SqlStatements BuildStatements(Type entityType)
	{
		var (schema, name) = ParseTableName(entityType, "dbo");
		var tableName = $"[{schema}].[{name}]";
		var columnMappings = GetColumnMappings(entityType);
		bool hasAlternateKey = HasAlternateKey(columnMappings);
		var columnsByPropertyName = columnMappings.ToDictionary(col => col.ParameterName);

		return new()
		{
			GetById = $"SELECT {SelectColumns(columnMappings)} FROM {tableName} WHERE [{columnsByPropertyName["Id"].ColumnName}]=@Id;",
			HasAlternateKey = hasAlternateKey,
			GetByAlternateKey = hasAlternateKey ? BuildGetByAlternateKey(tableName, columnMappings) : string.Empty,
			Insert = BuildInsert(tableName, columnMappings),
			Update = BuildUpdate(tableName, columnMappings),
			Delete = BuildDelete(tableName, columnMappings),
			ColumnMappings = columnMappings,
			TableName = tableName
		};
	}

	private static string SelectColumns(IEnumerable<ColumnMapping> columns) => string.Join(", ", columns.Select(col => $"[{col.ColumnName}]"));

	private static string BuildGetByAlternateKey(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var criteria = GetKeyColumnCriteria(tableName, columns, SetExpression);
		return $"SELECT {SelectColumns(columns)} FROM {tableName} WHERE {criteria};";
	}

	private static string BuildDelete(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var deleteCriteria = GetDeleteCriteria(columns, SetExpression);
		return $"DELETE {tableName} WHERE {deleteCriteria};";
	}

	private static string BuildUpdate(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var (setColumns, whereClause) = GetUpdateComponents(columns, SetExpression);
		return $"UPDATE {tableName} SET {setColumns} WHERE {whereClause};";
	}

	private static string BuildInsert(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var (names, values) = GetInsertComponents(columns, (col) => $"[{col.ColumnName}]");
		return $@"INSERT INTO {tableName} ({names}) OUTPUT [inserted].[Id] VALUES ({values});";
	}

	private static string SetExpression(ColumnMapping columnMapping) => $"[{columnMapping.ColumnName}]=@{columnMapping.ParameterName}";
}
