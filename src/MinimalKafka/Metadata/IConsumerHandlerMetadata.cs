using Confluent.Kafka;

namespace MinimalKafka.Metadata;
/// <summary>
/// Represents metadata for configuring various event handlers on a Kafka consumer.
/// </summary>
public interface IConsumerHandlerMetadata
{
    /// <summary>
    /// Gets the handler invoked when partitions are assigned to the consumer.
    /// </summary>
    /// <remarks>
    /// The handler receives the consumer instance and a list of assigned <see cref="TopicPartition"/>s,
    /// and returns the initial offsets for each partition.
    /// </remarks>
    Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionsAssignedHandler { get; }

    /// <summary>
    /// Gets the handler invoked when partitions are lost by the consumer.
    /// </summary>
    /// <remarks>
    /// The handler receives the consumer instance and a list of lost <see cref="TopicPartitionOffset"/>s,
    /// and returns the offsets to be committed or handled.
    /// </remarks>
    Func<object, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>? PartitionsLostHandler { get; }

    /// <summary>
    /// Gets the handler invoked when partitions are revoked from the consumer.
    /// </summary>
    /// <remarks>
    /// The handler receives the consumer instance and a list of revoked <see cref="TopicPartitionOffset"/>s.
    /// </remarks>
    Action<object, List<TopicPartitionOffset>>? PartitionsRevokedHandler { get; }

    /// <summary>
    /// Gets the handler invoked when the consumer receives statistics data.
    /// </summary>
    /// <remarks>
    /// The handler receives the consumer instance and a JSON string containing statistics.
    /// </remarks>
    Action<object, string>? StatisticsHandler { get; }

    /// <summary>
    /// Gets the handler invoked when an error occurs in the consumer.
    /// </summary>
    /// <remarks>
    /// The handler receives the consumer instance and the <see cref="Confluent.Kafka.Error"/> information.
    /// </remarks>
    Action<object, Error>? ErrorHandler { get; }

    /// <summary>
    /// Gets the handler invoked when a log message is produced by the consumer.
    /// </summary>
    /// <remarks>
    /// The handler receives the consumer instance and the <see cref="LogMessage"/> details.
    /// </remarks>
    Action<object, LogMessage>? LogHandler { get; }
}