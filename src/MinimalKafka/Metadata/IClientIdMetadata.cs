namespace MinimalKafka.Metadata;

/// <summary>
/// Represents metadata for specifying the Kafka client ID configuration.
/// </summary>
public interface IClientIdMetadata : IConsumerConfigMetadata, IProducerConfigMetadata
{
    /// <summary>
    /// Gets the client ID used to identify the Kafka client instance.
    /// </summary>
    string ClientId { get; }
}
