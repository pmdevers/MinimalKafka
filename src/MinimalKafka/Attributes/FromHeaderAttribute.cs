namespace MinimalKafka.Attributes;

/// <summary>Binds a handler parameter from a Kafka message header.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromHeaderAttribute : Attribute
{
    /// <summary>The header name to bind. When null, the parameter name is used.</summary>
    public string? Name { get; set; }
}
