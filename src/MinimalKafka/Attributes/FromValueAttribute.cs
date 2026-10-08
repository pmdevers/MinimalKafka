namespace MinimalKafka.Attributes;

/// <summary>Binds a handler parameter from the Kafka message value.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromValueAttribute : Attribute
{
}
