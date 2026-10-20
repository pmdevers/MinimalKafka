using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalKafka.Dashboard.Domain;
using MinimalKafka.Dashboard.Domain.Models;
using MinimalKafka.Runtime;

namespace MinimalKafka.Dashboard.Features;

internal static class GetOffsets
{

    internal const string Route = "/api/consumer-group/offsets";

    internal static async Task<IResult> HandleAsync(
        [FromServices] IOptions<MinimalKafkaDashboardOptions> dashboardOptions,
        [FromServices] IOptions<KafkaOptions> options,
        [FromServices] IKafkaDashboard dashboard,
        [FromServices] ITopicRegistry registry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = options.Value.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");

        var topics = registry.GetTopics();

        var topicPartitions = await dashboard.GetTopicPartitionsAsync(topics, cancellationToken);
        var offsets = await dashboard.GetConsumerGroupOffsetsAsync(groupId, topicPartitions, cancellationToken);

        return Results.Ok(new Response(groupId, offsets));
    }

    internal sealed record Response(string GroupId, IReadOnlyList<OffsetInfo> Offsets);
}
