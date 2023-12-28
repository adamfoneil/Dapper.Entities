using Dapper.Entities.Attributes;
using Dapper.Entities.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace Dapper.Entities.SqlServer;

public class DefaultSqlBuilder : ISqlBuilder
{
	public SqlStatements BuildStatements(Type entityType)
	{
		var (schema, name) = ParseTableName(entityType);
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

	private string BuildGetByAlternateKey(string tableName, Type entityType)
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
		var columns = GetColumnMappings(entityType, StatementType.Update);

		if (!columns.Any(col => col.IsKey)) throw new ArgumentException("Must have at least one key column");
		if (!columns.Any(col => col.ForUpdate)) throw new ArgumentException("Must have at least one update column");

		return
			$@"UPDATE {tableName} SET {string.Join(", ", columns.Where(col => col.ForUpdate).Select(SetExpression))}
            WHERE {string.Join(" AND ", columns.Where(UpdateOrDelete).Select(SetExpression))}";
	}

	private static bool UpdateOrDelete(ColumnMapping mapping) => mapping.IsKey && !mapping.ForUpdate;

	private static string BuildInsert(string tableName, Type entityType)
	{
		var columns = GetColumnMappings(entityType, StatementType.Insert);
		if (!columns.Any(col => col.ForInsert)) throw new ArgumentException("Must have at least one insert column");

		var insertCols = string.Join(", ", columns.Where(col => col.ForInsert).Select(col => $"[{col.ColumnName}]"));
		var insertValues = string.Join(", ", columns.Where(col => col.ForInsert).Select(col => $"@{col.ParameterName}"));

		return
			$@"INSERT INTO {tableName} (
                {insertCols}
            ) VALUES (
                {insertValues}
            ); SELECT SCOPE_IDENTITY()";
	}

	private static (string Schema, string Name) ParseTableName(Type type)
	{
		var schema = "dbo";
		var name = type.Name;

		if (type.HasAttribute<TableAttribute>(out var tableAttr))
		{
			if (!string.IsNullOrWhiteSpace(tableAttr.Schema))
			{
				schema = tableAttr.Schema;
			}

			if (!string.IsNullOrWhiteSpace(tableAttr.Name))
			{
				name = tableAttr.Name;
			}
		}

		return (schema, name);
	}

	private static IEnumerable<ColumnMapping> GetColumnMappings(Type entityType, StatementType statementType) =>
		entityType.GetProperties()
		.Where(pi => DefaultPropertyFilter(statementType, pi))
		.Select(DefaultColumnMapping);

	private static bool DefaultPropertyFilter(StatementType statementType, PropertyInfo propertyInfo)
	{
		if (!propertyInfo.CanRead) return false;
		if (propertyInfo.HasAttribute<NotMappedAttribute>(out _)) return false;
		if (statementType is StatementType.Insert && propertyInfo.HasAttribute<NotInsertedAttribute>(out _)) return false;
		if (statementType is StatementType.Update && propertyInfo.HasAttribute<NotUpdatedAttribute>(out _)) return false;

		if (propertyInfo.PropertyType.IsValueType) return true;
		if (propertyInfo.PropertyType.Equals(typeof(string))) return true;
		if (propertyInfo.PropertyType.Equals(typeof(Nullable<>)) && propertyInfo.PropertyType.GetGenericArguments().First().IsValueType) return true;

		return false;
	}

	private static ColumnMapping DefaultColumnMapping(PropertyInfo propertyInfo)
	{
		if (propertyInfo.Name.Equals(nameof(IEntity<int>.Id))) return new()
		{
			IsKey = true,
			ForUpdate = false,
			ForInsert = false,
			ColumnName = GetColumnName(propertyInfo),
			ParameterName = propertyInfo.Name
		};

		return new()
		{
			IsKey = propertyInfo.HasAttribute<KeyAttribute>(out _),
			ForUpdate = true,
			ForInsert = true,
			ColumnName = GetColumnName(propertyInfo),
			ParameterName = propertyInfo.Name
		};

		string GetColumnName(PropertyInfo propertyInfo)
		{
			var attr = propertyInfo.GetCustomAttribute<ColumnAttribute>();
			return (attr is not null && !string.IsNullOrEmpty(attr.Name)) ? attr.Name : propertyInfo.Name;
		}
	}

	private static string SetExpression(ColumnMapping columnMapping) => $"[{columnMapping.ColumnName}]=@{columnMapping.ParameterName}";

	private class ColumnMapping
	{
		public string ColumnName { get; set; } = string.Empty;
		public string ParameterName { get; set; } = string.Empty;
		public bool ForInsert { get; set; }
		public bool ForUpdate { get; set; }
		public bool IsKey { get; set; }
	}
}
