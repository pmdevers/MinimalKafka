namespace MinimalKafka.Metadata;

/// <summary>
/// Represents metadata for providing a deserializer factory for Kafka consumer keys or values.
/// </summary>
public interface IDeserializerMetadata
{
    /// <summary>
    /// Gets a factory function that produces a deserializer instance for use with a Kafka consumer builder.
    /// </summary>
    /// <remarks>
    /// The function receives an <see cref="IKafkaConsumerBuilder"/> and returns an object implementing the deserializer.
    /// </remarks>
    Func<IKafkaConsumerBuilder, object> Deserializer { get; }
}
