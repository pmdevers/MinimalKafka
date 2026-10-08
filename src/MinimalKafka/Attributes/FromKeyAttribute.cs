namespace MinimalKafka.Attributes;

/// <summary>Binds a handler parameter from the Kafka message key.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromKeyAttribute : Attribute
{
}
