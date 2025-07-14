namespace MinimalKafka;

/// <summary>
/// Provides a builder interface for constructing Kafka consumers.
/// Supports the creation of consumers that can work with multiple handlers.
/// </summary>
public interface IKafkaConsumerBuilder
{
    /// <summary>
    /// Builds and returns a configured Kafka consumer instance.
    /// </summary>
    /// <returns>A configured <see cref="IKafkaConsumer"/> instance.</returns>
    IKafkaConsumer Build();
}
