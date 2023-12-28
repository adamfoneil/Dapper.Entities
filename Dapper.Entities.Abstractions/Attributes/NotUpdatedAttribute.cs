namespace Dapper.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NotUpdatedAttribute : Attribute
{
}
