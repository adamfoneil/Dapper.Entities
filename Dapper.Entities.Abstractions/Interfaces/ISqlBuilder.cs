namespace Dapper.Entities.Interfaces;

public interface ISqlBuilder
{
	SqlStatements BuildStatements(Type entityType);
}

public class SqlStatements
{
	public string GetById { get; init; } = default!;
	public string GetByAlternateKey { get; init; } = default!;
	public string Insert { get; init; } = default!;
	public string Update { get; init; } = default!;
	public string Delete { get; init; } = default!;
}

