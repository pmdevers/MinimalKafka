using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MinimalKafka.Dashboard.Features;

internal static class ResetOffsets
{
    internal const string Route = "/api/consumer-group/offsets/reset";

    internal sealed record Request(IReadOnlyList<OffsetInfo> Offsets);
    internal sealed record OffsetInfo(string Topic, int Partition, long Offset);


    internal static async Task<IResult> HandleAsync(
        [FromServices] IOptions<MinimalKafkaDashboardOptions> dashboardOptions,
        [FromServices] IOptions<KafkaConsumerOptions> options,
        [FromBody] Request request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        var requestTimeout = dashboardOptions.Value.RequestTimeout;

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

        var config = options.Value.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");
        var adminConfig = options.Value.CreateAdminConfig();

        using var admin = new AdminClientBuilder(adminConfig).Build();
        var describeResult = await admin.DescribeConsumerGroupsAsync([groupId], new DescribeConsumerGroupsOptions
        {
            RequestTimeout = requestTimeout
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
                RequestTimeout = requestTimeout
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

        return Results.Ok(new Response(groupId, topicPartitionOffsets.Count));
    }

    internal sealed record Response(string GroupId, int UpdatedCount);
}
