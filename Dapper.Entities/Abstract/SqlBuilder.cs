using Dapper.Entities.Abstractions.Interfaces;
using Dapper.Entities.Abstractions.Models;
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

	protected static (string Schema, string Name) ParseTableName(Type type, string defaultSchema, Func<string, string>? nameFormatter = null)
	{
		var schema = defaultSchema;
		var name = type.Name;
		nameFormatter ??= name => name;

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

		return (nameFormatter(schema), nameFormatter(name));
	}

	protected static (string Columns, string Values) GetInsertComponents(IEnumerable<ColumnMapping> columns, Func<ColumnMapping, string> columnNameBuilder)
	{
		if (!columns.Any(col => col.ForInsert)) throw new ArgumentException("Must have at least one insert column");
		var insertCols = string.Join(", ", columns.Where(col => col.ForInsert).Select(col => columnNameBuilder(col)));
		var insertValues = string.Join(", ", columns.Where(col => col.ForInsert).Select(col => $"@{col.ParameterName}"));
		return (insertCols, insertValues);
	}

	protected static (string SetValues, string WhereClause) GetUpdateComponents(IEnumerable<ColumnMapping> columns, Func<ColumnMapping, string> expressionBuilder)
	{
		if (!columns.Any(col => col.IsKey)) throw new ArgumentException("Must have at least one key column");
		if (!columns.Any(col => col.ForUpdate)) throw new ArgumentException("Must have at least one update column");

		var setValues = string.Join(", ", columns.Where(col => col.ForUpdate).Select(col => expressionBuilder(col)));
		var whereClause = string.Join(" AND ", columns.Where(UpdateOrDelete).Select(col => expressionBuilder(col)));

		return (setValues, whereClause);
	}

	protected static bool UpdateOrDelete(ColumnMapping mapping) => mapping.IsKey && !mapping.ForUpdate;

	protected static string GetKeyColumnCriteria(string tableName, IEnumerable<ColumnMapping> columns, Func<ColumnMapping, string> columnNameBuilder)
	{
		var keyColumns = columns.Where(col => col.ForUpdate && col.IsKey).Select(columnNameBuilder);
		if (!keyColumns.Any()) throw new NotImplementedException($"Entity type for {tableName} is missing [Key] columns.");
		return string.Join(" AND ", keyColumns);
	}

	protected static string GetDeleteCriteria(IEnumerable<ColumnMapping> columns, Func<ColumnMapping, string> columnNameBuilder)
	{
		if (!columns.Any(col => col.IsKey)) throw new ArgumentException("Must have at least one key column");
		return string.Join(" AND ", columns.Where(UpdateOrDelete).Select(columnNameBuilder));
	}

	protected static ColumnMapping[] GetColumnMappings(Type entityType)
	{
		var results = entityType.GetProperties()
			.Where(DefaultPropertyFilter)
			.Select(DefaultColumnMapping)
			.ToArray();

		var instance = Activator.CreateInstance(entityType);
		if (instance is IAlternateKey altKey)
		{
			foreach (var col in results.Join(altKey.AlternateKeyColumns, col => col.ColumnName, val => val, (col, val) => col))
			{
				col.IsKey = true;
			}
		}

		return results;
	}

	protected static bool HasAlternateKey(IEnumerable<ColumnMapping> columnMappings) => columnMappings.Any(m => m.IsKey && m.ForUpdate);

	private static bool DefaultPropertyFilter(PropertyInfo propertyInfo)
	{
		if (!propertyInfo.CanRead) return false;
		if (propertyInfo.HasAttribute<NotMappedAttribute>(out _)) return false;

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
			ForUpdate = !propertyInfo.HasAttribute<NotUpdatedAttribute>(out _),
			ForInsert = !propertyInfo.HasAttribute<NotInsertedAttribute>(out _),
			ColumnName = GetColumnName(propertyInfo),
			ParameterName = propertyInfo.Name
		};

		static string GetColumnName(PropertyInfo propertyInfo)
		{
			var attr = propertyInfo.GetCustomAttribute<ColumnAttribute>();
			return (attr is not null && !string.IsNullOrEmpty(attr.Name)) ? attr.Name : propertyInfo.Name;
		}
	}	
}