using Dapper.Entities.Abstract;
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
			GetById = $"SELECT * FROM {tableName} WHERE [{columnsByPropertyName["Id"].ColumnName}]=@Id",
			HasAlternateKey = hasAlternateKey,
			GetByAlternateKey = hasAlternateKey ? BuildGetByAlternateKey(tableName, columnMappings) : string.Empty,
			Insert = BuildInsert(tableName, columnMappings),
			Update = BuildUpdate(tableName, columnMappings),
			Delete = BuildDelete(tableName, columnMappings)
		};
	}

	private static string BuildGetByAlternateKey(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var criteria = GetKeyColumnCriteria(tableName, columns, SetExpression);
		return $"SELECT * FROM {tableName} WHERE {criteria}";
	}

	private static string BuildDelete(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var deleteCriteria = GetDeleteCriteria(columns, SetExpression);
		return $"DELETE {tableName} WHERE {deleteCriteria}";
	}

	private static string BuildUpdate(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var (setColumns, whereClause) = GetUpdateComponents(columns, SetExpression);
		return $"UPDATE {tableName} SET {setColumns} WHERE {whereClause}";
	}

	private static string BuildInsert(string tableName, IEnumerable<ColumnMapping> columns)
	{
		var (names, values) = GetInsertComponents(columns, (col) => $"[{col.ColumnName}]");
		return $@"INSERT INTO {tableName} ({names}) VALUES ({values}); SELECT SCOPE_IDENTITY()";
	}

	private static string SetExpression(ColumnMapping columnMapping) => $"[{columnMapping.ColumnName}]=@{columnMapping.ParameterName}";
}
