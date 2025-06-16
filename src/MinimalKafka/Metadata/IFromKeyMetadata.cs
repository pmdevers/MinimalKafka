namespace MinimalKafka.Metadata;

/// <summary>
/// Marker interface indicating that a parameter or attribute is associated with the Kafka message key.
/// </summary>
/// <remarks>
/// This interface is typically implemented by attributes, such as <c>FromKeyAttribute</c>,
/// to signal that a method parameter should be bound to the key of a Kafka message.
/// </remarks>
public interface IFromKeyMetadata
{
}