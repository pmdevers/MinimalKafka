using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;

namespace MinimalKafka;

/// <summary>
/// Represents configuration options for creating and managing a Kafka consumer instance.
/// </summary>
public class KafkaConsumerOptions
{
    /// <summary>
    /// Gets or sets the type of the Kafka message key.
    /// </summary>
    public required Type KeyType { get; init; } = typeof(Ignore);

    /// <summary>
    /// Gets or sets the type of the Kafka message value.
    /// </summary>
    public required Type ValueType { get; init; } = typeof(Ignore);

    /// <summary>
    /// Gets or sets the name of the Kafka topic to consume from.
    /// </summary>
    public required string TopicName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of metadata objects used for configuring the consumer.
    /// </summary>
    public required IReadOnlyList<object> Metadata { get; init; } = [];

    /// <summary>
    /// Gets or sets the service provider used for dependency resolution.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; } = EmptyServiceProvider.Instance;

    /// <summary>
    /// Gets the logger instance for the Kafka consumer.
    /// </summary>
    public ILogger KafkaLogger => ServiceProvider.GetRequiredService<ILogger<KafkaConsumer>>();

    /// <summary>
    /// Handles the partition revoked event by logging the revoked partitions and returning the list.
    /// </summary>
    /// <param name="consumer">The Kafka consumer instance.</param>
    /// <param name="list">The list of revoked topic partition offsets.</param>
    /// <returns>The same list of <see cref="TopicPartitionOffset"/> that was provided.</returns>
    public IEnumerable<TopicPartitionOffset> PartitionsRevoked(IConsumer<object, object> consumer, List<TopicPartitionOffset> list)
    {
        KafkaLogger.PartitionsRevoked(consumer.MemberId, string.Join(",", list));
        return list;
    }
}
