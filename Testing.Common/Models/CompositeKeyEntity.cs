using Dapper.Entities.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Testing.Common.Models;

public class CompositeKeyEntity
{
	public int Id { get; set; }
	[Key]
	public int SomethingId { get; set; }
	[Key]
	public string Name { get; set; } = default!;
	public string Description { get; set; } = default!;
	[NotUpdated]
	public DateTime DateCreated { get; set; } = DateTime.Now;
	[NotInserted]
	public DateTime? DateModified { get; set; }
}
