namespace MinimalKafka.Metadata;

/// <summary>
/// Represents metadata for configuring the auto-commit behavior of a Kafka consumer.
/// </summary>
public interface IAutoCommitMetaData : IConsumerConfigMetadata
{
    /// <summary>
    /// Gets a value indicating whether auto-commit is enabled for the Kafka consumer.
    /// </summary>
    bool Enabled { get; }
}
