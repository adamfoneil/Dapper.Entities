using Dapper.Entities.Abstractions.Interfaces;
using Dapper.Entities.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Testing.Common.Models;

public class CompositeKeyEntity : IAlternateKey
{
	public int Id { get; set; }	
	public int SomethingId { get; set; }	
	public string Name { get; set; } = default!;
	public string Description { get; set; } = default!;
	[NotUpdated]
	public DateTime DateCreated { get; set; } = DateTime.Now;
	[NotInserted]
	public DateTime? DateModified { get; set; }

	public IEnumerable<string> AlternateKeyColumns => [ nameof(SomethingId), nameof(Name) ];
}

/// <summary>
/// this version is simply to ensure no regression of earlier column mapping IsKey setting
/// </summary>
[Table("CompositeKeyEntity")]
public class CompositeKeyEntity2
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