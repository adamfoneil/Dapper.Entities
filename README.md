SqlServer package:
[![Nuget](https://img.shields.io/nuget/v/Dapper.Entities.SqlServer)](https://www.nuget.org/packages/Dapper.Entities.SqlServer/)

PostgreSql package:
[![Nuget](https://img.shields.io/nuget/v/Dapper.Entities.PostgreSql)](https://www.nuget.org/packages/Dapper.Entities.PostgreSql/)

This is a minimal ORM framework that uses Dapper and a repository pattern approach. The only hard dependency on your entity classes is that they implement [IEntity\<TKey\>](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities.Abstractions/Interfaces/IEntity.cs). It lets you write code like this (Blazor):

```csharp
@page "/Business/{Id:int}"
@inject MyDatabase Db

<EditForm Model="model" OnValidSubmit="Save">
  // markup omitted for clarity
</EditForm>

@code {
Entities.Business model = new();

[Parameter] public int Id { get; set; }

protected override async OnInitializedAsync()
{
    if (Id is not 0)
    {
        model = await Db.Business.GetAsync(Id);
    }
}

async Task Save() => await Db.Business.SaveAsync(model);

}
```

Service `MyDatabase` is injected, giving access to any number of database tables. In this example it's using a `Business` table and calling `GetAsync` to fetch a row, and `SaveAsync` to insert or update a row.

# Walkthrough
1. Start by creating a class that derives from `SqlServerDatabase`, passing a connection string and `ILogger` in the constructor.

<details>
  <summary>Code</summary>

```csharp
public class MyDatabase : SqlServerDatabase
{
    public MyDatabase(string connectionString, ILogger<MyDatabase> logger) : base(connectionString, logger)
    {
    }

    // todo: add Repository properties for the tables in your database
}
```
</details>

2. Add a `Repository` class that encapsulates conventions the tables in your database follow. In this example, I'm setting a convention that all my tables will have `int` keys, but you can choose any struct type you want. If there's business logic that applies to all or most tables, it would go in this class as well. There are many overrides you can implement to customize repository behavior, adding trigger-like behavior, permission checks, and multi-tenant isolation, for example. This is a bare-bones example below.
 
<details>
  <summary>Code</summary>

```csharp
public class BaseRepository<TEntity> : Repository<MyDatabase, TEntity, int> where TEntity : IEntity<int>
{
    public BaseRepository(MyDatabase database) : base(database)
    {            
    }
}
```
</details>

3. Go back and add repository properties to your `MyDatabase` class like this. In this example, I'm adding a `Business` repository along with `Another` and `YetAnother`. These should be model classes in your application. The `Business` example comes from the test [here](https://github.com/adamfoneil/Dapper.Entities/blob/master/Testing.Common/Data/Entities/Business.cs).

<details>
  <summary>Code</summary>

```csharp
public class MyDatabase : SqlServerDatabase
{
    public MyDatabase(string connectionString, ILogger<MyDatabase> logger) : base(connectionString, logger)
    {
    }

    // todo: add Repository properties for the tables in your database
    public BaseRepository<Business> Business => new(this);
    public BaseRepository<AnotherTable> Another => new(this);
    public BaseRepository<YetAnotherTable> YetAnother => new(this);
}
```
</details>

# Entity class considerations
The only requirement for entity classes you use with this library is that they implement [IEntity\<TKey\>](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities.Abstractions/Interfaces/IEntity.cs). This gives your entity classes an `Id` property in the struct type of your choice.

You can use the `[NotMapped]` attribute on columns that don't save directly to your database table. Likewise, you can also use `[NotUpdated]` and `[NotInserted]` to get finer control on column save behavior.

Use the `[Key]` attribute on any combination of properties to define an entity's alternate key. This lets you take advantage of the [MergeAsync](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities/Repository.cs#L121) method, which searches for an existing row before inserting a new row. Do not use `[Key]` on the `Id` property. It's already understood to be a key. In my tests, notice I use the `[Key]` attribute [here](https://github.com/adamfoneil/Dapper.Entities/blob/master/Testing.Common/Data/Entities/Business.cs#L8-L9) to define uniqueness of the `UserId` property.

# SQL generation
The low-level `Database` class (from which the SQL Server implementation derives) [constructor](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities/Database.cs#L7) accepts an [ISqlBuilder](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities.Abstractions/Interfaces/ISqlBuilder.cs). This is responsible for generating the SQL statements used by `Repository` classes. I offer a default implementation [DefaultSqlBuilder](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities.SqlServer/DefaultSqlBuilder.cs) for SQL Server. You can implement this yourself to generate SQL however you like. My implementation is a bare-bones approach that does not do concurrency checking, for example.

Note that you can also use [stored procedures or completely custom SQL](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities/Repository.cs#L31-L39) for select repositories.

There's also a [PostgreSql implementation](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities.PostgreSql/DefaultSqlBuilder.cs). As of this writing, I don't have any Postgres experience to speak of. I wanted to show that this architecture could work for a variety of backend databases, and might even start using Postgres for some things.

# Next Steps
In the walkthrough above, I have a single `BaseRepository` assumed to be used with all tables. In a realistic application, you'd have tables with unique business logic such as trigger-like behavior, permission checks, validation, change tracking, audit tracking, and so on. This library doesn't provide any of that capability built-in. Rather, this library provides many virtual methods in the [Repository](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities/Repository.cs) class such as `BeforeSaveAsync`, `AfterSaveAsync`, `BeforeDeleteAsync` to let you richly [customize](https://github.com/adamfoneil/Dapper.Entities/blob/master/Dapper.Entities/Repository.cs#L192-L206) your data access.

Check out my [Ensync](https://github.com/adamfoneil/Ensync) project to see how you can do code-first entity development in SQL Server without migrations.

# Background
This is an evolution of [Dapper.Repository](https://github.com/adamfoneil/Dapper.Repository), which I feel has gotten a bit complicated due to tight integration with authentication. I felt it was time to drop back and refactor, rethink some dependencies, and re-architect this from scratch. I've probably made two dozen or more ORM libraries over my career, so this is definitely a weird obsession I have. Crafting ORM libraries is one of those things devs are told not to do because the ORM problem is well-solved by much smarter people using very mature, well-tested libraries. But a truly great dev experience with data in C# remains somewhat elusive, in my opinion. I've played with EF Core a bit more than usual lately, and that's part of what's driving this effort. The truth is that I really *do not enjoy* working with EF Core. ALthough I've made some peace with migrations, having practiced some more, I still run into too many *gotchas* and annoyances with EF.

Note that as a "minimal" library, this is focused on CRUD operations only. It's not a general purpose query library nor intended to compete with LINQ, for example. (I love LINQ!) There are many interesting query helper libraries out there. I have my own that uses Dapper internally as well [Dapper.QX](https://github.com/adamfoneil/Dapper.QX).
