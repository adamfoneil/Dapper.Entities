using System.ComponentModel.DataAnnotations;
using Testing.Data.Conventions;

namespace Testing.Data.Entities;

public class Business : BaseTable
{
	[Key]
	public string UserId { get; set; } = default!;
	public string DisplayName { get; set; } = default!;
	public string MailingAddress { get; set; } = default!;
	public string PhoneNumber { get; set; } = default!;
	public string Email { get; set; } = default!;
	public int NextInvoiceNumber { get; set; } = 1000;
}
