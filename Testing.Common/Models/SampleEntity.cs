using System.ComponentModel.DataAnnotations.Schema;

namespace Testing.Models;

[Table("Sample", Schema = "whatever")]
public class SampleEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = default!;
	public string Description { get; set; } = default!;
	[NotMapped]
	public decimal Calculated { get; set; }
}
