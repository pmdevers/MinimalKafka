using Confluent.Kafka;

namespace MinimalKafka.Metadata.Internals;
internal class ConsumerHandlerMetadata : IConsumerHandlerMetadata
{
    public Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> PartitionsAssignedHandler { get; set; }
        = (_, _) => [];
    public Func<object, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> PartitionsLostHandler { get; set; } 
        = (_, _) => [];
    public Action<object, List<TopicPartitionOffset>> PartitionsRevokedHandler { get; set; }
        = (_, _) => { };
    public Action<object, string> StatisticsHandler { get; set; }
        = (_, _) => { };
    public Action<object, Error> ErrorHandler { get; set; }
        = (_, _) => { };
    public Action<object, LogMessage> LogHandler { get; set; }
        = (_, _) => { };
}