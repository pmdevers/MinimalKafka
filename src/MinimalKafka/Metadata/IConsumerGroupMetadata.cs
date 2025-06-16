namespace MinimalKafka.Metadata;

/// <summary>
/// Represents metadata for specifying the Kafka consumer group ID configuration.
/// </summary>
public interface IGroupIdMetadata
{
    /// <summary>
    /// Gets the consumer group ID used to identify the Kafka consumer group.
    /// </summary>
    string GroupId { get; }
}
