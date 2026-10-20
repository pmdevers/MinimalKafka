using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalKafka.Dashboard.Domain;
using MinimalKafka.Dashboard.Domain.Models;

namespace MinimalKafka.Dashboard.Features;

internal static class GetMembers
{
    internal const string Route = "/api/consumer-group/members";

    internal static async Task<IResult> HandleAsync(
        [FromServices] IOptions<KafkaOptions> options,
        [FromServices] IKafkaDashboard dashboard,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var config = options.Value.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");

        var members = await dashboard.DescribeConsumerGroupsAsync([groupId], cancellationToken).ConfigureAwait(false);

        return Results.Json(new Response(groupId, members));
    }

    internal sealed record Response(string GroupId, IReadOnlyList<Member> Members);

}