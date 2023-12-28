using Dapper.Entities.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace Testing.Models;

public class ExoticEntity
{
	public Guid Id { get; set; }
	public string? Name { get; set; }
	public decimal? Value { get; set; }
	[Column("Aliased")]
	public bool AliasedColumn { get; set; }
	[NotMapped]
	public Point Location { get; set; }
	[NotUpdated]
	public DateTime DateCreated { get; set; }
	[NotInserted]
	public DateTime? DateModified { get; set; }

	public IEnumerable<int> Whatevers { get; set; } = Enumerable.Empty<int>();
}
