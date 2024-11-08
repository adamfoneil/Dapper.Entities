namespace Dapper.Entities.Interfaces;

public interface IEntity<TKey> where TKey : notnull
{
	TKey Id { get; set; }
}

public static class EntityExtensions
{
	public static bool IsNew<TKey>(this IEntity<TKey> entity) where TKey : notnull => entity.Id.Equals(default(TKey));
}
