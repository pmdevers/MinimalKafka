using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MinimalKafka.Dashboard.Features;

internal static class GetMembers
{
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(10);

    internal const string Route = "/api/consumer-group/members";

    internal static async Task<IResult> HandleAsync(
        [FromServices] IOptions<KafkaOptions> options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = options.Value.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");
        var adminConfig = options.Value.CreateAdminConfig();

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

        var offsets = group.Members
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

        return Results.Json(new Response(groupId, offsets));
    }

    internal sealed record Response(string GroupId, IReadOnlyList<Member> Members);
    internal sealed record Member(
        string MemberId,
        string ClientId,
        string? GroupInstanceId,
        string ClientHost,
        IReadOnlyList<string> AssignedPartitions);
}
