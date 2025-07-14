using Confluent.Kafka;

namespace MinimalKafka.Metadata;

/// <summary>
/// Provides access to Kafka consumer and producer configuration metadata.
/// </summary>
public interface IConfigMetadata
{

    /// <summary>
    /// Gets the consumer configuration for Kafka operations.
    /// </summary>
    ConsumerConfig ConsumerConfig { get; }

    /// <summary>
    /// Gets the producer configuration for Kafka operations.
    /// </summary>
    ProducerConfig ProducerConfig { get; }

}