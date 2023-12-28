using System.Data;
using Testing.Data.Entities;

namespace Testing.SqlServer;

[TestClass]
public class Integration : IntegrationBase
{	
	[TestMethod]
	public async Task InsertUpdateDelete()
	{
		var db = GetDatabase();
		using var cn = db.GetConnection();
		await TestInternalAsync(db, cn);
	}

	private static async Task TestInternalAsync(LiteInvoiceDatabase db, IDbConnection cn, IDbTransaction? txn = null)
	{	
		var result = await db.Business.SaveAsync(cn, new Business()
		{
			UserId = "hello",
			DisplayName = "anything",
			MailingAddress = "3983 Axelrod",
			PhoneNumber = "294-2322",
			Email = "whatever@nowhere.org"
		}, txn);

		Assert.IsTrue(result.Id != 0);
		Assert.IsTrue(result.UserId.Equals("hello"));

		result.UserId = "someoneelse";
		await db.Business.SaveAsync(cn, result, txn);

		result = await db.Business.GetAsync(cn, result.Id, txn) ?? throw new Exception("row not found");
		Assert.IsTrue(result.UserId.Equals("someoneelse"));

		await db.Business.DeleteAsync(cn, new Business() { Id = result.Id }, txn);
	}

	[TestMethod]
	public async Task InsertUpdateDeleteWithTxn()
	{
		var db = GetDatabase();
		await db.DoTransactionAsync(async (cn, txn) =>
		{
			await TestInternalAsync(db, cn, txn);
		});
	}
}
