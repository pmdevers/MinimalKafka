using MinimalKafka.Dashboard.Domain.Models;

namespace MinimalKafka.Dashboard.Domain;

internal interface IKafkaDashboard
{
    Task<Member[]> DescribeConsumerGroupsAsync(string[] groupIds, CancellationToken cancellationToken);
    Task<OffsetInfo[]> GetConsumerGroupOffsetsAsync(string groupId, TopicPartition[] topicPartitions, CancellationToken cancellationToken);
    Task<TopicPartition[]> GetTopicPartitionsAsync(string[] topics, CancellationToken cancellationToken);
    Task<TopicMessage[]> GetTopicMessagesAsync(string topic, int count, CancellationToken cancellationToken);
    Task<int> ResetConsumerGroupOffsetsAsync(string groupId, ConsumerOffsetReset[] offsets, CancellationToken cancellationToken);
}
