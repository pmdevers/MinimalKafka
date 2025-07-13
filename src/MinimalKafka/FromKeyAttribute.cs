using MinimalKafka.Metadata;

namespace MinimalKafka;

/// <summary>
/// Indicates that a method parameter should be bound to the Kafka message key.
/// </summary>
/// <remarks>
/// Apply this attribute to a parameter in a Kafka handler method to specify that the parameter
/// should receive the key from the incoming Kafka message. This is typically used in conjunction
/// with MinimalKafka's delegate-based handler registration.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
public sealed class FromKeyAttribute : Attribute, IFromKeyMetadata
{
}
