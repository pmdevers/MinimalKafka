using Confluent.Kafka;

namespace MinimalKafka.Metadata;

/// <summary>
/// Defines a contract for setting client configuration options on a <see cref="ClientConfig"/> instance.
/// </summary>
public interface IClientConfigMetadata
{
    /// <summary>
    /// Applies configuration settings to the specified <see cref="ClientConfig"/> instance.
    /// </summary>
    /// <param name="config">The client configuration to modify.</param>
    void Set(ClientConfig config);
}

/// <summary>
/// Represents metadata for configuring Kafka consumer-specific options.
/// Inherits from <see cref="IClientConfigMetadata"/>.
/// </summary>
public interface IConsumerConfigMetadata : IClientConfigMetadata
{
}

/// <summary>
/// Represents metadata for configuring Kafka producer-specific options.
/// Inherits from <see cref="IClientConfigMetadata"/>.
/// </summary>
public interface IProducerConfigMetadata : IClientConfigMetadata
{
}

/// <summary>
/// Represents metadata for configuring the <see cref="AutoOffsetReset"/> policy of a Kafka consumer.
/// </summary>
public interface IAutoOffsetResetMetadata
{
    /// <summary>
    /// Gets the <see cref="AutoOffsetReset"/> policy to be applied to the consumer.
    /// </summary>
    AutoOffsetReset AutoOffsetReset { get; }
}
