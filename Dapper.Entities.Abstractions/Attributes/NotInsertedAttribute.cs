namespace Dapper.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NotInsertedAttribute : Attribute
{
}
