using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalKafka.Runtime;

namespace MinimalKafka.Dashboard.Features;

internal static class GetOffsets
{

    internal const string Route = "/api/consumer-group/offsets";

    internal static async Task<IResult> HandleAsync(
        [FromServices] IOptions<MinimalKafkaDashboardOptions> dashboardOptions,
        [FromServices] IOptions<KafkaConsumerOptions> options,
        [FromServices] ITopicRegistry registry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestTimeout = dashboardOptions.Value.RequestTimeout;

        var config = options.Value.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");
        var adminConfig = options.Value.CreateAdminConfig();

        var topics = registry.GetTopics();
        using var admin = new AdminClientBuilder(adminConfig).Build();

        var topicPartitions = topics
            .SelectMany(topic => admin.GetMetadata(topic, requestTimeout).Topics
                .Where(metadataTopic => string.Equals(metadataTopic.Topic, topic, StringComparison.Ordinal) && !metadataTopic.Error.IsError)
                .SelectMany(metadataTopic => metadataTopic.Partitions
                    .Select(metadataPartition => new TopicPartition(topic, new Partition(metadataPartition.PartitionId)))))
            .ToList();

        var offsetsResult = await admin.ListConsumerGroupOffsetsAsync(
            [new ConsumerGroupTopicPartitions(groupId, topicPartitions)],
            new ListConsumerGroupOffsetsOptions
            {
                RequestTimeout = requestTimeout,
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
                var watermark = consumer.QueryWatermarkOffsets(offset.TopicPartition, requestTimeout);
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

        return Results.Ok(new Response(groupId, offsets));
    }

    internal sealed record Response(string GroupId, IReadOnlyList<OffsetInfo> Offsets);
    internal sealed record OffsetInfo(string Topic, int Partition, long? CommittedOffset, long? HighOffset, long? Lag);
}
