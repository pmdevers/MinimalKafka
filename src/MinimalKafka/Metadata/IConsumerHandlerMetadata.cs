using Confluent.Kafka;

namespace MinimalKafka.Metadata;

public interface IConsumerHandlerMetadata
{
    Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionsAssignedHandler { get; }
    Func<object, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>? PartitionsLostHandler { get; }
    Action<object, List<TopicPartitionOffset>>? PartitionsRevokedHandler { get; }
    Action<object, string>? StatisticsHandler { get; }
    Action<object, Error>? ErrorHandler { get; }
    Action<object, LogMessage>? LogHandler { get; }
}
