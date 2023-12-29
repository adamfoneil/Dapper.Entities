using Dapper.Entities.Abstract;
using Dapper.Entities.Interfaces;
using System.Data;

namespace Dapper.Entities.SqlServer;

public class DefaultSqlBuilder : SqlBuilder
{
	public override SqlStatements BuildStatements(Type entityType)
	{
		var (schema, name) = ParseTableName(entityType, "dbo");
		var tableName = $"[{schema}].[{name}]";

		return new()
		{
			GetById = $"SELECT * FROM {tableName} WHERE [Id] = @id",
			GetByAlternateKey = BuildGetByAlternateKey(tableName, entityType),
			Insert = BuildInsert(tableName, entityType),
			Update = BuildUpdate(tableName, entityType),
			Delete = BuildDelete(tableName, entityType)
		};
	}

	private static string BuildGetByAlternateKey(string tableName, Type entityType)
	{
		var columns = GetColumnMappings(entityType, StatementType.Update);

		var keyColumns = columns.Where(col => col.ForUpdate && col.IsKey).Select(SetExpression);
		if (!keyColumns.Any()) return string.Empty;

		var criteria = string.Join(" AND ", keyColumns);

		return $"SELECT * FROM {tableName} WHERE {keyColumns}";
	}

	private static string BuildDelete(string tableName, Type entityType)
	{
		var columns = GetColumnMappings(entityType, StatementType.Delete);

		if (!columns.Any(col => col.IsKey)) throw new ArgumentException("Must have at least one key column");

		return $"DELETE {tableName} WHERE {string.Join(" AND ", columns.Where(UpdateOrDelete).Select(SetExpression))}";
	}

	private static string BuildUpdate(string tableName, Type entityType)
	{
		var (setColumns, whereClause) = GetUpdateComponents(entityType, SetExpression);
		return $"UPDATE {tableName} SET {setColumns} WHERE {whereClause}";
	}	

	private static string BuildInsert(string tableName, Type entityType)
	{
		var (columns, values) = GetInsertComponents(entityType, (col) => $"[{col.ColumnName}]");
		return $@"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT SCOPE_IDENTITY()";
	}

	private static string SetExpression(ColumnMapping columnMapping) => $"[{columnMapping.ColumnName}]=@{columnMapping.ParameterName}";	
}
