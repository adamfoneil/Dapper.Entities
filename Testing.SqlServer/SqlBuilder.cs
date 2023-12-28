using Dapper.Entities.SqlServer;
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

		Assert.IsTrue(statements.Insert.Equals(
			@"INSERT INTO [whatever].[Sample] (
                [Name], [Description]
            ) VALUES (
                @Name, @Description
            ); SELECT SCOPE_IDENTITY()"));

		Assert.IsTrue(statements.Update.Equals(
			@"UPDATE [whatever].[Sample] SET [Name]=@Name, [Description]=@Description
            WHERE [Id]=@Id"));

		Assert.IsTrue(statements.Delete.Equals("DELETE [whatever].[Sample] WHERE [Id]=@Id"));
	}

	[TestMethod]
	public void ExoticEntity()
	{
		var builder = new DefaultSqlBuilder();
		var statements = builder.BuildStatements(typeof(ExoticEntity));

		Assert.IsTrue(statements.Insert.Equals(
			@"INSERT INTO [dbo].[ExoticEntity] (
                [Name], [Value], [Aliased], [DateCreated]
            ) VALUES (
                @Name, @Value, @AliasedColumn, @DateCreated
            ); SELECT SCOPE_IDENTITY()"));

		Assert.IsTrue(statements.Update.Equals(
			@"UPDATE [dbo].[ExoticEntity] SET [Name]=@Name, [Value]=@Value, [Aliased]=@AliasedColumn, [DateModified]=@DateModified
            WHERE [Id]=@Id"));
	}
}