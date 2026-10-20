using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MinimalKafka.Dashboard.Domain;

namespace MinimalKafka.Dashboard.Features;

internal static class ResetOffsets
{
    internal const string Route = "/api/consumer-group/offsets/reset";

    internal sealed record Request(IReadOnlyList<OffsetInfo> Offsets);
    internal sealed record OffsetInfo(string Topic, int Partition, long Offset);


    internal static async Task<IResult> HandleAsync(
        [FromServices] IOptions<KafkaOptions> options,
        [FromServices] IKafkaDashboard dashboard,
        [FromBody] Request request,
        CancellationToken cancellationToken)
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

        var config = options.Value.CreateConsumerConfig();
        var groupId = config.GroupId ?? throw new InvalidOperationException("Kafka consumer group id is required for the dashboard.");

        var updatedCount = await dashboard.ResetConsumerGroupOffsetsAsync(
            groupId,
            request.Offsets
                .Select(offset => new Domain.Models.ConsumerOffsetReset(offset.Topic, offset.Partition, offset.Offset))
                .ToArray(),
            cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);

        return Results.Ok(new Response(groupId, updatedCount));
    }

    internal sealed record Response(string GroupId, int UpdatedCount);
}
