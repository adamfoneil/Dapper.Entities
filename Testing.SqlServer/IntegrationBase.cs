using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac;
using Serilog;
using Serilog.Extensions.Logging;
using SqlServer.LocalDb;
using System.Data;
using System.Reflection;

namespace Testing.SqlServer;

public class IntegrationBase
{
	protected const string DbName = "DapperEntitiesTesting";

	[ClassInitialize]
	public static void CreateSampleDb(TestContext context)
	{
		using var cn = LocalDb.GetConnection(DbName);
		cn.Close();

		using var stream = GetResource("Testing.SqlServer.Resources.LiteInvoice.dacpac");
		using var package = DacPackage.Load(stream);
		var services = new DacServices(LocalDb.GetConnectionString(DbName));
		services.Deploy(package, DbName, true);
	}

	[ClassCleanup]
	public static async Task DeleteSampleDb()
	{
		using var cn = LocalDb.GetConnection(DbName);
		await cn.ExecuteAsync("DELETE [dbo].[Business]");
	}

	protected static IDbConnection GetConnection() => LocalDb.GetConnection(DbName);

	protected static LiteInvoiceDatabase GetDatabase()
	{
		var serilogLogger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.WriteTo.Debug()
			.CreateLogger();

		var logger = new SerilogLoggerFactory(serilogLogger)
			.CreateLogger<LiteInvoiceDatabase>();

		/*var logger = LoggerFactory.Create(config =>
		{
			config.AddConsole();			
			config.SetMinimumLevel(LogLevel.Trace);
		}).CreateLogger<LiteInvoiceDatabase>();*/

		return new LiteInvoiceDatabase(LocalDb.GetConnectionString(DbName), logger);
	}

	protected static Stream GetResource(string name) => Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new Exception($"Resource {name} not found");
}
