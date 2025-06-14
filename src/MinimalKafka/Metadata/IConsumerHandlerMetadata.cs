using Confluent.Kafka;

namespace MinimalKafka.Metadata;

public interface IConsumerHandlerMetadata
{
    Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionsAssignedHandler { get; set; }

    Action<object, string>? StatisticsHandler { get; set;}
    Action<object, Error>? ErrorHandler { get;  set; }
}

internal class ConsumerHandlerMetadata : IConsumerHandlerMetadata
{
    public Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionsAssignedHandler { get; set; }
    public Action<object, string>? StatisticsHandler { get; set; }
    public Action<object, Error>? ErrorHandler { get; set; }
}
