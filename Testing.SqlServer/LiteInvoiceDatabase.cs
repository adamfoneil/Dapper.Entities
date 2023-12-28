using Dapper.Entities.SqlServer;
using Microsoft.Extensions.Logging;
using Testing.Data.Entities;

namespace Testing.SqlServer;

public class LiteInvoiceDatabase : SqlServerDatabase
{
	public LiteInvoiceDatabase(string connectionString, ILogger<LiteInvoiceDatabase> logger) : base(connectionString, logger)
	{
	}

	public BaseRepository<Business> Business => new(this);
}
