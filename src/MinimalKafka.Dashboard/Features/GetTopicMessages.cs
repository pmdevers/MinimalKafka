using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalKafka.Runtime;
using System.Text;

namespace MinimalKafka.Dashboard.Features;

internal static class GetTopicMessages
{
    internal const string Route = "/api/topics/messages";

    internal static async Task<IResult> HandleAsync(
        [FromServices] IOptions<MinimalKafkaDashboardOptions> dashboardOptions,
        [FromServices] IOptions<KafkaOptions> options,
        [FromServices] ITopicRegistry registry,
        [FromQuery] string topic,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestTimeout = dashboardOptions.Value.RequestTimeout;

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new InvalidOperationException("Topic is required.");
        }

        var normalizedCount = count <= 0 ? 50 : Math.Min(count, 500);
        var registeredTopics = registry.GetTopics();
        if (!registeredTopics.Contains(topic, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Topic '{topic}' is not registered.");
        }

        var config = options.Value.CreateConsumerConfig();
        var adminConfig = options.Value.CreateAdminConfig();

        using var admin = new AdminClientBuilder(adminConfig).Build();
        var topicMetadata = admin.GetMetadata(topic, requestTimeout).Topics
            .FirstOrDefault(metadataTopic => string.Equals(metadataTopic.Topic, topic, StringComparison.Ordinal) && !metadataTopic.Error.IsError);

        if (topicMetadata is null || topicMetadata.Partitions.Count == 0)
        {
            return Results.Ok(Response.Empty(topic, normalizedCount));
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
            var watermark = consumer.QueryWatermarkOffsets(topicPartition, requestTimeout);
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
            return Results.Ok(Response.Empty(topic, normalizedCount));
        }

        consumer.Assign(assignments);

        var captured = new List<Message>();
        var reachedEnd = new HashSet<TopicPartition>();
        var deadline = DateTime.UtcNow + requestTimeout;

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

                captured.Add(new Message(
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

        return Results.Ok(new Response(topic, normalizedCount, latest));
    }

    internal sealed record Response(string Topic, int RequestedCount, IReadOnlyList<Message> Messages)
    {
        public static Response Empty(string topic, int requestedCount) => new(topic, requestedCount, []);
    };

    internal sealed record Message(string Topic, int Partition, long Offset, string Value, DateTimeOffset Timestamp);
}
