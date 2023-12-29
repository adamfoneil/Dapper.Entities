using Dapper.Entities.PostgreSql;
using Testing.Common.Models;
using Testing.Models;

namespace Testing.PostgreSql;

[TestClass]
public class SqlBuilder
{
	[TestMethod]
	public void SampleEntity()
	{
		var builder = new DefaultSqlBuilder() { CaseConversion = CaseConversionOptions.SnakeCase };
		var statements = builder.BuildStatements(typeof(SampleEntity));

		Assert.IsTrue(statements.GetById.Equals(
			"SELECT id AS Id, name AS Name, description AS Description FROM whatever.sample WHERE id = @id"));

		Assert.IsTrue(statements.Insert.Equals(
			"INSERT INTO whatever.sample (name, description) VALUES (@Name, @Description) RETURNING id;"));

		Assert.IsTrue(statements.Update.Equals(
			"UPDATE whatever.sample SET name=@Name, description=@Description WHERE id=@Id"));

		Assert.IsTrue(statements.Delete.Equals(
			"DELETE FROM whatever.sample WHERE id=@Id"));

		Assert.IsTrue(statements.HasAlternateKey);

		Assert.IsTrue(statements.GetByAlternateKey.Equals(
			"SELECT id AS Id, name AS Name, description AS Description FROM whatever.sample WHERE name=@Name"));
	}

	[TestMethod]
	public void ExoticEntity()
	{
		var builder = new DefaultSqlBuilder() { CaseConversion = CaseConversionOptions.SnakeCase };
		var statements = builder.BuildStatements(typeof(ExoticEntity));

		Assert.IsTrue(statements.GetById.Equals(
			"SELECT exotic_id AS Id, name AS Name, value AS Value, aliased AS AliasedColumn, date_created AS DateCreated, date_modified AS DateModified FROM public.exotic_entity WHERE exotic_id = @id"));

		Assert.IsTrue(statements.Insert.Equals(
			"INSERT INTO public.exotic_entity (name, value, aliased, date_created) VALUES (@Name, @Value, @AliasedColumn, @DateCreated) RETURNING id;"));

		Assert.IsTrue(statements.Update.Equals(
			"UPDATE public.exotic_entity SET name=@Name, value=@Value, aliased=@AliasedColumn, date_modified=@DateModified WHERE exotic_id=@Id"));

		Assert.IsTrue(statements.Delete.Equals(
			"DELETE FROM public.exotic_entity WHERE exotic_id=@Id"));

		Assert.IsFalse(statements.HasAlternateKey);

		Assert.IsTrue(statements.GetByAlternateKey.Equals(string.Empty));
	}

	[TestMethod]
	public void CompositeEntity()
	{
		var builder = new DefaultSqlBuilder() { CaseConversion = CaseConversionOptions.SnakeCase };
		var statements = builder.BuildStatements(typeof(CompositeKeyEntity));

		Assert.IsTrue(statements.Insert.Equals(
			"INSERT INTO public.composite_key_entity (something_id, name, description, date_created) VALUES (@SomethingId, @Name, @Description, @DateCreated) RETURNING id;"));

		Assert.IsTrue(statements.Update.Equals(
			"UPDATE public.composite_key_entity SET something_id=@SomethingId, name=@Name, description=@Description, date_modified=@DateModified WHERE id=@Id"));

		Assert.IsTrue(statements.Delete.Equals(
			"DELETE FROM public.composite_key_entity WHERE id=@Id"));

		Assert.IsTrue(statements.HasAlternateKey);

		Assert.IsTrue(statements.GetByAlternateKey.Equals(
			"SELECT id AS Id, something_id AS SomethingId, name AS Name, description AS Description, date_created AS DateCreated, date_modified AS DateModified FROM public.composite_key_entity WHERE something_id=@SomethingId AND name=@Name"));
	}
}