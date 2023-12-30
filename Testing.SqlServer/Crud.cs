using Dapper;
using Dapper.Entities.Extensions;
using Dapper.Entities.SqlServer;
using Testing.Data.Entities;

namespace Testing.SqlServer;

[TestClass]
public class Crud : IntegrationBase
{
	[TestMethod]
	public async Task BasicOps()
	{
		CrudExtensions.SqlBuilder = new DefaultSqlBuilder();

		using var cn = GetConnection();
		using var txn = cn.BeginTransaction();
		
		var biz = await cn.InsertAsync<Business, long>(new()
		{
			DisplayName = "WhateverThis",
			Email = "anyone@nowhere.org",
			UserId = "whatever2",
			DateCreated = DateTime.Now
		}, txn);

		var savedId = biz.Id;
		Assert.IsTrue(savedId != 0);

		biz = await cn.GetAsync<Business, long>(savedId, txn) ?? throw new Exception("row not found");
		Assert.IsTrue(savedId == biz.Id);

		biz.Email = "anyone2@nowhere.org";
		biz.UserId = "whatever3";
		await cn.UpdateAsync<Business, long>(biz, txn);

		biz = await cn.GetAsync<Business, long>(biz.Id, txn) ?? throw new Exception("row not found");
		Assert.IsTrue(biz.Email.Equals("anyone2@nowhere.org"));
		Assert.IsTrue(biz.UserId.Equals("whatever3"));

		await cn.DeleteAsync<Business, long>(biz.Id, txn);

		biz = await cn.GetAsync<Business, long>(biz.Id, txn);
		Assert.IsNull(biz);
	}

	[TestMethod]
	public async Task SaveAndMerge()
	{
		CrudExtensions.SqlBuilder = new DefaultSqlBuilder();
		
		using var cn = GetConnection();

		const string KeyValue = "Whatever4";

		await cn.ExecuteAsync("DELETE [dbo].[Business] WHERE [UserId]=@userId", new { userId = KeyValue });

		var biz = await cn.SaveAsync<Business, long>(new()
		{
			DisplayName = "Save and Merge Test",
			Email = "anyone@nowhere.org",
			UserId = KeyValue,
			DateCreated = DateTime.Now
		});

		var insertedId = biz.Id;
		Assert.IsTrue(insertedId != 0);

		// perform a merge against the key value we started with, should be the original row
		var merge = await cn.MergeAsync<Business, long>(new()
		{
			UserId = KeyValue,
			DisplayName = "Save and Merge Test 2",
			Email = "anyone@nowhere.org"
		}, onExisting: (newRow, existingRow) =>
		{
			newRow.DateModified = DateTime.Now;
		});

		var mergedId = merge.Id;
		Assert.IsTrue(mergedId == insertedId);

		var mergedRow = await cn.GetAsync<Business, long>(mergedId) ?? throw new Exception("row not found");
		Assert.AreEqual(mergedRow.DisplayName, "Save and Merge Test 2");
		Assert.IsTrue(mergedRow.DateModified.HasValue);
	}
}
