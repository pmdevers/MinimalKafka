namespace MinimalKafka.Attributes;

/// <summary>Binds a handler parameter from the request service provider.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromServicesAttribute : Attribute
{
}
