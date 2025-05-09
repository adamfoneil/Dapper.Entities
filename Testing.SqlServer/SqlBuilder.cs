using Dapper.Entities.SqlServer;
using Testing.Common.Models;
using Testing.Models;

namespace Testing.SqlServer;

[TestClass]
public class SqlBuilder
{
	[TestMethod]
	public void SampleEntity()
	{
		var builder = new DefaultSqlBuilder();
		var statements = builder.BuildStatements(typeof(SampleEntity));

		Assert.IsTrue(statements.GetById.Equals(
			"SELECT [Id], [Name], [Description] FROM [whatever].[Sample] WHERE [Id]=@Id;"));

		Assert.IsTrue(statements.Insert.Equals(
			"INSERT INTO [whatever].[Sample] ([Name], [Description]) OUTPUT [inserted].[Id] VALUES (@Name, @Description);"));

		Assert.IsTrue(statements.Update.Equals(
			"UPDATE [whatever].[Sample] SET [Name]=@Name, [Description]=@Description WHERE [Id]=@Id;"));

		Assert.IsTrue(statements.Delete.Equals(
			"DELETE [whatever].[Sample] WHERE [Id]=@Id;"));

		Assert.IsTrue(statements.HasAlternateKey);

		Assert.IsTrue(statements.GetByAlternateKey.Equals(
			"SELECT [Id], [Name], [Description] FROM [whatever].[Sample] WHERE [Name]=@Name;"));
	}

	[TestMethod]
	public void ExoticEntity()
	{
		var builder = new DefaultSqlBuilder();
		var statements = builder.BuildStatements(typeof(ExoticEntity));

		Assert.IsTrue(statements.GetById.Equals(
			"SELECT [ExoticId], [Name], [Value], [Aliased], [DateCreated], [DateModified] FROM [dbo].[ExoticEntity] WHERE [ExoticId]=@Id;"));

		Assert.IsTrue(statements.Insert.Equals(
			"INSERT INTO [dbo].[ExoticEntity] ([Name], [Value], [Aliased], [DateCreated]) OUTPUT [inserted].[Id] VALUES (@Name, @Value, @AliasedColumn, @DateCreated);"));

		Assert.IsTrue(statements.Update.Equals(
			"UPDATE [dbo].[ExoticEntity] SET [Name]=@Name, [Value]=@Value, [Aliased]=@AliasedColumn, [DateModified]=@DateModified WHERE [ExoticId]=@Id;"));

		Assert.IsTrue(statements.Delete.Equals(
			"DELETE [dbo].[ExoticEntity] WHERE [ExoticId]=@Id;"));

		Assert.IsFalse(statements.HasAlternateKey);

		Assert.IsTrue(statements.GetByAlternateKey.Equals(string.Empty));
	}

	[TestMethod]
	public void CompositeEntity()
	{
		var builder = new DefaultSqlBuilder();

		foreach (var type in new[] { typeof(CompositeKeyEntity), typeof(CompositeKeyEntity2) })
		{
			var statements = builder.BuildStatements(type);

			Assert.IsTrue(statements.Insert.Equals(
				"INSERT INTO [dbo].[CompositeKeyEntity] ([SomethingId], [Name], [Description], [DateCreated]) OUTPUT [inserted].[Id] VALUES (@SomethingId, @Name, @Description, @DateCreated);"));

			Assert.IsTrue(statements.Update.Equals(
				"UPDATE [dbo].[CompositeKeyEntity] SET [SomethingId]=@SomethingId, [Name]=@Name, [Description]=@Description, [DateModified]=@DateModified WHERE [Id]=@Id;"));

			Assert.IsTrue(statements.Delete.Equals(
				"DELETE [dbo].[CompositeKeyEntity] WHERE [Id]=@Id;"));

			Assert.IsTrue(statements.HasAlternateKey);

			Assert.IsTrue(statements.GetByAlternateKey.Equals(
				"SELECT [Id], [SomethingId], [Name], [Description], [DateCreated], [DateModified] FROM [dbo].[CompositeKeyEntity] WHERE [SomethingId]=@SomethingId AND [Name]=@Name;"));
		}
	}
}