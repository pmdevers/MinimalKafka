using MinimalKafka.Dashboard.Contracts;

namespace MinimalKafka.Dashboard.Services;

internal interface IKafkaDashboardQueryService
{
    Task<ConsumerGroupMembersResponse> GetMembersAsync(CancellationToken cancellationToken);

    Task<ConsumerGroupOffsetsResponse> GetOffsetsAsync(CancellationToken cancellationToken);

    Task<ConsumerGroupOffsetResetResponse> ResetOffsetsAsync(ConsumerGroupOffsetResetRequest request, CancellationToken cancellationToken);

    Task<TopicMessagesResponse> GetTopicMessagesAsync(string topic, int count, CancellationToken cancellationToken);

    ConsumerStatusResponse GetConsumerStatus();

    ConsumerStatusResponse StartConsumers();

    ConsumerStatusResponse StopConsumers();
}
