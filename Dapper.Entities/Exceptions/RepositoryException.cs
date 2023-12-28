namespace Dapper.Entities.Exceptions;

public class RepositoryException : Exception
{
	public RepositoryException(RepositoryAction action, string message, Exception innerException) : base(message, innerException)
	{
		Action = action;
	}

	public RepositoryException(RepositoryAction action, string message) : base(message)
	{
		Action = action;
	}

	public RepositoryAction Action { get; }
	public string? Sql { get; init; }
	public object? Parameters { get; init; }
}
