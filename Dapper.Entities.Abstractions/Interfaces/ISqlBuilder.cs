using Dapper.Entities.Abstractions.Models;

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
}

