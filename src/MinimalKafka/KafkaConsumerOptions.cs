using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MinimalKafka;

public class KafkaConsumerOptions
{
    public required Type KeyType { get; init; } = typeof(Ignore);
    public required Type ValueType { get; init; } = typeof(Ignore);
    public required string TopicName { get; init; } = string.Empty;
    public required IReadOnlyList<object> Metadata { get; init; } = [];
    public required IServiceProvider ServiceProvider { get; init; } = EmptyServiceProvider.Instance;
    public ILogger KafkaLogger => ServiceProvider.GetRequiredService<ILogger<KafkaConsumer>>();

    public IEnumerable<TopicPartitionOffset> PartitionsRevoked(IConsumer<object, object> consumer, List<TopicPartitionOffset> list)
    {
        KafkaLogger.LogInformation("consumer {MemberId} had partitions {Partitions} revoked"
            , consumer.MemberId, string.Join(",", list));
        return list;
    }
}
