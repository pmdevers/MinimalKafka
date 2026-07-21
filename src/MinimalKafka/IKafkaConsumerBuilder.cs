namespace MinimalKafka;

/// <summary>
/// Provides a builder interface for constructing Kafka consumers.
/// Supports the creation of consumers that can work with multiple handlers.
/// </summary>
public interface IKafkaConsumerBuilder
{
    /// <summary>
    /// Sets the key for the Kafka consumer.
    /// </summary>
    /// <param name="key">The key to be used by the Kafka consumer.</param>
    /// <returns>The current <see cref="IKafkaConsumerBuilder"/> instance.</returns>
    IKafkaConsumerBuilder WithKey(KafkaConsumerKey key);

    /// <summary>
    /// Sets the metadata for the Kafka consumer.
    /// </summary>
    /// <param name="metadata">The metadata to be associated with the Kafka consumer.</param>
    /// <returns>The current <see cref="IKafkaConsumerBuilder"/> instance.</returns>
    IKafkaConsumerBuilder WithMetadata(IReadOnlyList<object> metadata);

    /// <summary>
    /// Builds and returns a configured Kafka consumer instance.
    /// </summary>
    /// <returns>A configured <see cref="IKafkaConsumer"/> instance.</returns>
    IKafkaConsumer Build();
}
