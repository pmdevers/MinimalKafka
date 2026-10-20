using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using MinimalKafka.Dashboard.Domain;
using MinimalKafka.Dashboard.Domain.Models;
using System.Text;

namespace MinimalKafka.Dashboard.Infrastructure;

internal sealed class KafkaDashboard(IOptions<KafkaOptions> options) : IKafkaDashboard
{
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(10);
    private readonly IAdminClient _adminClient = new AdminClientBuilder(options.Value.CreateAdminConfig())
        .SetOAuthBearerTokenRefreshHandler(options.Value.Handlers.OAuthBearerTokenRefreshHandler)
        .Build();

    private readonly IConsumer<byte[], byte[]> _consumer = new ConsumerBuilder<byte[], byte[]>(options.Value.CreateConsumerConfig())
        .SetOAuthBearerTokenRefreshHandler(options.Value.Handlers.OAuthBearerTokenRefreshHandler)
        .Build();

    public async Task<Member[]> DescribeConsumerGroupsAsync(string[] groupIds, CancellationToken cancellationToken)
    {
        var result = await _adminClient.DescribeConsumerGroupsAsync(groupIds, new DescribeConsumerGroupsOptions
        {
            RequestTimeout = _requestTimeout
        }).WaitAsync(cancellationToken).ConfigureAwait(false);

        var group = result.ConsumerGroupDescriptions.Single();
        if (group.Error.IsError)
        {
            throw new InvalidOperationException($"Unable to describe consumer group '{group.GroupId}': {group.Error.Reason}");
        }

        return group.Members
            .Select(member => new Member(
                member.ConsumerId,
                member.ClientId,
                member.GroupInstanceId,
                member.Host,
                member.Assignment.TopicPartitions
                    .Select(topicPartition => $"{topicPartition.Topic}[{topicPartition.Partition.Value}]")
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray()))
            .OrderBy(member => member.ClientId, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<OffsetInfo[]> GetConsumerGroupOffsetsAsync(string groupId, Domain.Models.TopicPartition[] topicPartitions, CancellationToken cancellationToken)
    {
        var tp = topicPartitions.Select(value => new Confluent.Kafka.TopicPartition(value.Topic, value.Partition)).ToList();
        var offsetsResult = await _adminClient.ListConsumerGroupOffsetsAsync(
            [new ConsumerGroupTopicPartitions(groupId, tp)],
            new ListConsumerGroupOffsetsOptions
            {
                RequestTimeout = _requestTimeout,
                RequireStableOffsets = false
            }).WaitAsync(cancellationToken).ConfigureAwait(false);

        var groupOffsets = offsetsResult.Single();
        var partitionErrors = groupOffsets.Partitions
            .Where(offset => offset.Error.IsError)
            .Select(offset => $"{offset.Topic}[{offset.Partition.Value}]: {offset.Error.Reason}")
            .ToArray();

        if (partitionErrors.Length > 0)
        {
            throw new InvalidOperationException(
                $"Unable to get offsets for consumer group '{groupId}'. Partition errors: {string.Join(", ", partitionErrors)}");
        }

        return groupOffsets.Partitions
            .Select(offset =>
            {
                var watermark = _consumer.QueryWatermarkOffsets(offset.TopicPartition, _requestTimeout);
                var high = watermark.High.Value;
                var committedOffset = offset.Offset >= Offset.Beginning ? offset.Offset.Value : 0;
                var lag = high >= committedOffset ? high - committedOffset : 0;

                return new OffsetInfo(
                    offset.Topic,
                    offset.Partition.Value,
                    committedOffset,
                    high,
                    lag);
            })
            .OrderBy(entry => entry.Topic, StringComparer.Ordinal)
            .ThenBy(entry => entry.Partition)
            .ToArray();
    }

    public Task<Domain.Models.TopicPartition[]> GetTopicPartitionsAsync(string[] topics, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var partitions = topics
            .SelectMany(topic => _adminClient.GetMetadata(topic, _requestTimeout).Topics
                .Where(metadataTopic => string.Equals(metadataTopic.Topic, topic, StringComparison.Ordinal) && !metadataTopic.Error.IsError)
                .SelectMany(metadataTopic => metadataTopic.Partitions
                    .Select(metadataPartition => new Domain.Models.TopicPartition(topic, metadataPartition.PartitionId))))
            .ToArray();

        return Task.FromResult(partitions);
    }

    public async Task<int> ResetConsumerGroupOffsetsAsync(string groupId, ConsumerOffsetReset[] offsets, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var describeResult = await _adminClient.DescribeConsumerGroupsAsync([groupId], new DescribeConsumerGroupsOptions
        {
            RequestTimeout = _requestTimeout
        }).WaitAsync(cancellationToken).ConfigureAwait(false);

        var group = describeResult.ConsumerGroupDescriptions.Single();
        if (group.Error.IsError)
        {
            throw new InvalidOperationException($"Unable to describe consumer group '{groupId}': {group.Error.Reason}");
        }

        if (group.Members.Count > 0)
        {
            throw new InvalidOperationException($"Consumer group '{groupId}' has active members. Stop all consumers before resetting offsets.");
        }

        var topicPartitionOffsets = offsets
            .Select(offset => new TopicPartitionOffset(
                new Confluent.Kafka.TopicPartition(offset.Topic, new Partition(offset.Partition)),
                new Offset(offset.Offset)))
            .ToList();

        var alterResult = await _adminClient.AlterConsumerGroupOffsetsAsync(
            [new ConsumerGroupTopicPartitionOffsets(groupId, topicPartitionOffsets)],
            new AlterConsumerGroupOffsetsOptions
            {
                RequestTimeout = _requestTimeout
            }).WaitAsync(cancellationToken).ConfigureAwait(false);

        var groupResult = alterResult.Single();
        var errors = groupResult.Partitions
            .Where(offset => offset.Error.IsError)
            .Select(offset => $"{offset.Topic}[{offset.Partition.Value}]: {offset.Error.Reason}")
            .ToArray();

        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                $"Unable to reset offsets for consumer group '{groupId}'. Partition errors: {string.Join(", ", errors)}");
        }

        return topicPartitionOffsets.Count;
    }

    public Task<TopicMessage[]> GetTopicMessagesAsync(string topic, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var topicMetadata = _adminClient.GetMetadata(topic, _requestTimeout).Topics
            .FirstOrDefault(metadataTopic => string.Equals(metadataTopic.Topic, topic, StringComparison.Ordinal) && !metadataTopic.Error.IsError);

        if (topicMetadata is null || topicMetadata.Partitions.Count == 0)
        {
            return Task.FromResult(Array.Empty<TopicMessage>());
        }

        var endOffsets = new Dictionary<Confluent.Kafka.TopicPartition, long>();
        var assignments = new List<TopicPartitionOffset>();

        foreach (var partitionMetadata in topicMetadata.Partitions)
        {
            var topicPartition = new Confluent.Kafka.TopicPartition(topic, new Partition(partitionMetadata.PartitionId));
            var watermark = _consumer.QueryWatermarkOffsets(topicPartition, _requestTimeout);
            var low = watermark.Low.Value;
            var high = watermark.High.Value;

            if (high <= low)
            {
                continue;
            }

            var start = Math.Max(low, high - count);
            assignments.Add(new TopicPartitionOffset(topicPartition, new Offset(start)));
            endOffsets[topicPartition] = high;
        }

        if (assignments.Count == 0)
        {
            return Task.FromResult(Array.Empty<TopicMessage>());
        }

        _consumer.Assign(assignments);

        var captured = new List<TopicMessage>();
        var reachedEnd = new HashSet<Confluent.Kafka.TopicPartition>();
        var deadline = DateTime.UtcNow + _requestTimeout;

        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline && reachedEnd.Count < endOffsets.Count)
        {
            var result = _consumer.Consume(TimeSpan.FromMilliseconds(200));
            if (result is null)
            {
                continue;
            }

            if (!endOffsets.TryGetValue(result.TopicPartition, out var endOffsetExclusive))
            {
                continue;
            }

            if (result.Offset >= Offset.Beginning)
            {
                var timestamp = result.Message.Timestamp.UnixTimestampMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(result.Message.Timestamp.UnixTimestampMs)
                    : DateTimeOffset.MinValue;

                captured.Add(new TopicMessage(
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    Encoding.UTF8.GetString(result.Message.Value ?? []),
                    timestamp));
            }

            if (result.Offset.Value + 1 >= endOffsetExclusive)
            {
                reachedEnd.Add(result.TopicPartition);
            }
        }

        var latest = captured
            .OrderByDescending(message => message.Timestamp)
            .ThenByDescending(message => message.Offset)
            .Take(count)
            .OrderBy(message => message.Timestamp)
            .ThenBy(message => message.Partition)
            .ThenBy(message => message.Offset)
            .ToArray();

        return Task.FromResult(latest);
    }
}
