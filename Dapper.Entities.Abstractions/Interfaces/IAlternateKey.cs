namespace Dapper.Entities.Abstractions.Interfaces;

public interface IAlternateKey
{
	IEnumerable<string> AlternateKeyColumns { get; }
}
