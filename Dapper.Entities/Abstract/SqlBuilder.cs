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
