using Dapper.Entities.Attributes;
using Dapper.Entities.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace Dapper.Entities.Abstract;

public abstract class SqlBuilder : ISqlBuilder
{
	public abstract SqlStatements BuildStatements(Type entityType);

	protected static (string Schema, string Name) ParseTableName(Type type, string defaultSchema)
	{
		var schema = defaultSchema;
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

	protected static (string Columns, string Values) GetInsertComponents(Type entityType, Func<ColumnMapping, string> columnNameBuilder)
	{
		var columns = GetColumnMappings(entityType, StatementType.Insert).Where(col => col.ForInsert).ToArray();
		if (!columns.Any(col => col.ForInsert)) throw new ArgumentException("Must have at least one insert column");

		var insertCols = string.Join(", ", columns.Select(col => columnNameBuilder(col)));
		var insertValues = string.Join(", ", columns.Select(col => $"@{col.ParameterName}"));

		return (insertCols, insertValues);
	}

	protected static (string SetValues, string WhereClause) GetUpdateComponents(Type entityType, Func<ColumnMapping, string> expressionBuilder)
	{
		var columns = GetColumnMappings(entityType, StatementType.Update);

		if (!columns.Any(col => col.IsKey)) throw new ArgumentException("Must have at least one key column");
		if (!columns.Any(col => col.ForUpdate)) throw new ArgumentException("Must have at least one update column");

		var setValues = string.Join(", ", columns.Where(col => col.ForUpdate).Select(col => expressionBuilder(col)));
		var whereClause = string.Join(" AND ", columns.Where(UpdateOrDelete).Select(col => expressionBuilder(col)));

		return (setValues, whereClause);
	}

	protected static bool UpdateOrDelete(ColumnMapping mapping) => mapping.IsKey && !mapping.ForUpdate;

	protected static IEnumerable<ColumnMapping> GetColumnMappings(Type entityType, StatementType statementType) =>
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

		static string GetColumnName(PropertyInfo propertyInfo)
		{
			var attr = propertyInfo.GetCustomAttribute<ColumnAttribute>();
			return (attr is not null && !string.IsNullOrEmpty(attr.Name)) ? attr.Name : propertyInfo.Name;
		}
	}

	protected class ColumnMapping
	{
		public string ColumnName { get; set; } = string.Empty;
		public string ParameterName { get; set; } = string.Empty;
		public bool ForInsert { get; set; }
		public bool ForUpdate { get; set; }
		public bool IsKey { get; set; }
	}
}
