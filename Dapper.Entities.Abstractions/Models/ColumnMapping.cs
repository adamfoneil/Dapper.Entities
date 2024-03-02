namespace Dapper.Entities.Abstractions.Models;

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
