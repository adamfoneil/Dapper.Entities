using Dapper.Entities.Attributes;
using Dapper.Entities.Interfaces;

namespace Testing.Data.Conventions;

public abstract class BaseTable : IEntity<long>
{
	public long Id { get; set; }
	[NotUpdated]
	public DateTime DateCreated { get; set; }
	[NotInserted]
	public DateTime? DateModified { get; set; }
}
