using Dapper;
using Npgsql;
using System.Data;

namespace Testing.PostgreSql;

public class IntegrationBase
{
	protected static async Task CreateSampleTable()
	{
		using var cn = GetConnection();

		try
		{
			await cn.ExecuteAsync(
				@"CREATE TABLE public.business (
					id BIGSERIAL PRIMARY KEY,
					user_id varchar(50) NOT NULL,
					display_name varchar(50),
					mailing_address varchar(50),
					phone_number varchar(20),
					email varchar(50),
					next_invoice_number int NOT NULL DEFAULT (1000),
					date_created timestamp without time zone NOT NULL,
					date_modified timestamp without time zone NULL,
					UNIQUE (user_id)
				)");
		}
		catch
		{
			// ignore
		}
	}

	protected static async Task DeleteSampleTable()
	{
		using var cn = GetConnection();
		await cn.ExecuteAsync("DROP TABLE public.business");
	}

	protected static IDbConnection GetConnection()
	{
		var cn = new NpgsqlConnection("Host=localhost;Port=5432;Database=DapperEntitiesTest;User Id=postgres;Password=hello.304");
		cn.Open();
		return cn;
	}
}
