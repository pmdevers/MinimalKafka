namespace MinimalKafka.Metadata;

/// <summary>
/// Marker interface indicating that a parameter or attribute is associated with the Kafka message value.
/// </summary>
/// <remarks>
/// This interface is typically implemented by attributes, such as <c>FromValueAttribute</c>,
/// to signal that a method parameter should be bound to the value of a Kafka message.
/// </remarks>
public interface IFromValueMetadata
{
}
