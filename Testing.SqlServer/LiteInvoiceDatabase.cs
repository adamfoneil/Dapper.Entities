using Dapper.Entities.SqlServer;
using Microsoft.Extensions.Logging;
using Testing.Data.Entities;

namespace Testing.SqlServer;

public class LiteInvoiceDatabase(string connectionString, ILogger<LiteInvoiceDatabase> logger) : SqlServerDatabase(connectionString, logger)
{
	public BaseRepository<Business> Business => new(this);
}
