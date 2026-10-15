using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using MinimalKafka.Dashboard.Contracts;
using MinimalKafka.Runtime;
using System.Text;

namespace MinimalKafka.Dashboard.Services;

internal sealed class KafkaDashboardQueryService(
    IOptions<KafkaConsumerOptions> options,
    ITopicRegistry registry,
    IConsumerRuntimeControl runtimeControl)
    : IKafkaDashboardQueryService
{
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(10);
    private readonly KafkaConsumerOptions _options = options.Value;
    private readonly ITopicRegistry _registry = registry;
    private readonly IConsumerRuntimeControl _runtimeControl = runtimeControl;

    public async Task<ConsumerGroupMembersResponse> GetMembersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = _options.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");
        var adminConfig = _options.CreateAdminConfig();

        using var admin = new AdminClientBuilder(adminConfig).Build();
        var result = await admin.DescribeConsumerGroupsAsync([groupId], new DescribeConsumerGroupsOptions
        {
            RequestTimeout = _requestTimeout
        }).WaitAsync(cancellationToken).ConfigureAwait(false);

        var group = result.ConsumerGroupDescriptions.Single();
        if (group.Error.IsError)
        {
            throw new InvalidOperationException($"Unable to describe consumer group '{groupId}': {group.Error.Reason}");
        }

        var members = group.Members
            .Select(member => new ConsumerGroupMemberDto(
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

        return new ConsumerGroupMembersResponse(groupId, members);
    }

    public async Task<ConsumerGroupOffsetsResponse> GetOffsetsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = _options.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");
        var adminConfig = _options.CreateAdminConfig();

        var topics = _registry.GetTopics();
        using var admin = new AdminClientBuilder(adminConfig).Build();

        var topicPartitions = topics
            .SelectMany(topic => admin.GetMetadata(topic, _requestTimeout).Topics
                .Where(metadataTopic => string.Equals(metadataTopic.Topic, topic, StringComparison.Ordinal) && !metadataTopic.Error.IsError)
                .SelectMany(metadataTopic => metadataTopic.Partitions
                    .Select(metadataPartition => new TopicPartition(topic, new Partition(metadataPartition.PartitionId)))))
            .ToList();

        var offsetsResult = await admin.ListConsumerGroupOffsetsAsync(
            [new ConsumerGroupTopicPartitions(groupId, topicPartitions)],
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

        using var consumer = new ConsumerBuilder<byte[], byte[]>(config)
            .SetKeyDeserializer(Deserializers.ByteArray)
            .SetValueDeserializer(Deserializers.ByteArray)
            .Build();

        var offsets = groupOffsets.Partitions
            .Select(offset =>
            {
                var watermark = consumer.QueryWatermarkOffsets(offset.TopicPartition, _requestTimeout);
                var high = watermark.High.Value;
                var committedOffset = offset.Offset >= Offset.Beginning ? offset.Offset.Value : 0;
                var lag = high >= committedOffset ? high - committedOffset : 0;

                return new ConsumerGroupTopicOffsetDto(
                    offset.Topic,
                    offset.Partition.Value,
                    committedOffset,
                    high,
                    lag);
            })
            .OrderBy(entry => entry.Topic, StringComparer.Ordinal)
            .ThenBy(entry => entry.Partition)
            .ToArray();

        return new ConsumerGroupOffsetsResponse(groupId, offsets);
    }

    public async Task<ConsumerGroupOffsetResetResponse> ResetOffsetsAsync(ConsumerGroupOffsetResetRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        if (request.Offsets.Count == 0)
        {
            throw new InvalidOperationException("At least one topic/partition offset reset item is required.");
        }

        var duplicate = request.Offsets
            .GroupBy(offset => (offset.Topic, offset.Partition))
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate offset reset item detected for topic '{duplicate.Key.Topic}' partition '{duplicate.Key.Partition}'.");
        }

        var invalid = request.Offsets.FirstOrDefault(offset =>
            string.IsNullOrWhiteSpace(offset.Topic) ||
            offset.Partition < 0 ||
            offset.Offset < 0);

        if (invalid is not null)
        {
            throw new InvalidOperationException("Offset reset items must include topic, non-negative partition, and non-negative offset values.");
        }

        var config = _options.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");
        var adminConfig = _options.CreateAdminConfig();

        using var admin = new AdminClientBuilder(adminConfig).Build();
        var describeResult = await admin.DescribeConsumerGroupsAsync([groupId], new DescribeConsumerGroupsOptions
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

        var topicPartitionOffsets = request.Offsets
            .Select(offset => new TopicPartitionOffset(
                new TopicPartition(offset.Topic, new Partition(offset.Partition)),
                new Offset(offset.Offset)))
            .ToList();

        var alterResult = await admin.AlterConsumerGroupOffsetsAsync(
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

        return new ConsumerGroupOffsetResetResponse(groupId, topicPartitionOffsets.Count);
    }

    public async Task<TopicMessagesResponse> GetTopicMessagesAsync(string topic, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new InvalidOperationException("Topic is required.");
        }

        var normalizedCount = count <= 0 ? 50 : Math.Min(count, 500);
        var registeredTopics = _registry.GetTopics();
        if (!registeredTopics.Contains(topic, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Topic '{topic}' is not registered.");
        }

        var config = _options.CreateConsumerConfig();
        var adminConfig = _options.CreateAdminConfig();

        using var admin = new AdminClientBuilder(adminConfig).Build();
        var topicMetadata = admin.GetMetadata(topic, _requestTimeout).Topics
            .FirstOrDefault(metadataTopic => string.Equals(metadataTopic.Topic, topic, StringComparison.Ordinal) && !metadataTopic.Error.IsError);

        if (topicMetadata is null || topicMetadata.Partitions.Count == 0)
        {
            return new TopicMessagesResponse(topic, normalizedCount, []);
        }

        using var consumer = new ConsumerBuilder<byte[], byte[]>(config)
            .SetKeyDeserializer(Deserializers.ByteArray)
            .SetValueDeserializer(Deserializers.ByteArray)
            .Build();

        var endOffsets = new Dictionary<TopicPartition, long>();
        var assignments = new List<TopicPartitionOffset>();

        foreach (var partitionMetadata in topicMetadata.Partitions)
        {
            var topicPartition = new TopicPartition(topic, new Partition(partitionMetadata.PartitionId));
            var watermark = consumer.QueryWatermarkOffsets(topicPartition, _requestTimeout);
            var low = watermark.Low.Value;
            var high = watermark.High.Value;

            if (high <= low)
            {
                continue;
            }

            var start = Math.Max(low, high - normalizedCount);
            assignments.Add(new TopicPartitionOffset(topicPartition, new Offset(start)));
            endOffsets[topicPartition] = high;
        }

        if (assignments.Count == 0)
        {
            return new TopicMessagesResponse(topic, normalizedCount, []);
        }

        consumer.Assign(assignments);

        var captured = new List<TopicMessageDto>();
        var reachedEnd = new HashSet<TopicPartition>();
        var deadline = DateTime.UtcNow + _requestTimeout;

        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline && reachedEnd.Count < endOffsets.Count)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(200));
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

                captured.Add(new TopicMessageDto(
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
            .Take(normalizedCount)
            .OrderBy(message => message.Timestamp)
            .ThenBy(message => message.Partition)
            .ThenBy(message => message.Offset)
            .ToArray();

        return new TopicMessagesResponse(topic, normalizedCount, latest);
    }

    public ConsumerStatusResponse GetConsumerStatus() => new(_runtimeControl.IsRunning);

    public ConsumerStatusResponse StartConsumers()
    {
        _runtimeControl.Start();
        return new ConsumerStatusResponse(_runtimeControl.IsRunning);
    }

    public ConsumerStatusResponse StopConsumers()
    {
        _runtimeControl.Stop();
        return new ConsumerStatusResponse(_runtimeControl.IsRunning);
    }
}
