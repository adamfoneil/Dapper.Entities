using Dapper;
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

	[TestMethod]
	public async Task UpdateSpecificColumns()
	{
		var db = GetDatabase();

		using var cn = db.GetConnection();
		await cn.ExecuteAsync("DELETE [dbo].[Business] WHERE [UserId] = 'hello1121'");

		var biz = await db.Business.SaveAsync(new Business()
		{
			UserId = "hello1121",
			DisplayName = "anything",
			MailingAddress = "3983 Axelrod",
			PhoneNumber = "294-2322",
			Email = "this.email@nowhere.org"
		});

		await db.Business.UpdateAsync(biz.Id, row =>
		{
			row.PhoneNumber = "123-4567";
			row.MailingAddress = null;
		});

		biz = await db.Business.GetAsync(biz.Id) ?? throw new Exception("row not found");
		Assert.AreEqual(biz.PhoneNumber, "123-4567");
		Assert.IsTrue(biz.MailingAddress is null);
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
