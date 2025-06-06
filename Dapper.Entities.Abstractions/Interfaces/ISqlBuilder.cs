namespace Dapper.Entities.Interfaces;

public interface ISqlBuilder
{
	SqlStatements BuildStatements(Type entityType);
}

public class SqlStatements
{
	public string GetById { get; init; } = default!;
	public bool HasAlternateKey { get; init; }
	public string GetByAlternateKey { get; init; } = default!;
	public string Insert { get; init; } = default!;
	public string Update { get; init; } = default!;
	public string Delete { get; init; } = default!;
	public ColumnMapping[] ColumnMappings { get; init; } = [];
	public string TableName { get; init; } = default!;	
	public required Func<IEnumerable<string>, string> UpdateColumns { get; init; }
}

public class ColumnMapping
{
	/// <summary>
	/// this is usually the C# property, but can be aliased with the [Column] attribute
	/// </summary>
	public string ColumnName { get; init; } = string.Empty;
	/// <summary>
	/// this is always the C# property name
	/// </summary>
	public string ParameterName { get; init; } = string.Empty;
	public bool ForInsert { get; init; }
	public bool ForUpdate { get; init; }
	public bool IsKey { get; set; }
}


