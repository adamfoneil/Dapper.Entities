using Dapper.Entities.Extensions;
using Dapper.Entities.PostgreSql;
using Testing.Data.Entities;

namespace Testing.PostgreSql;

[TestClass]
public class Crud : IntegrationBase
{
	[ClassInitialize]
	public static async Task Initialize(TestContext context) => await CreateSampleTable();

	[ClassCleanup]
	public static async Task Cleanup() => await DeleteSampleTable();

	[TestMethod]
	public async Task BasicOps()
	{
		CrudExtensions.SqlBuilder = new DefaultSqlBuilder() { CaseConversion = CaseConversionOptions.SnakeCase };

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
}
