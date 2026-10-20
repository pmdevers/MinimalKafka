using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Dashboard.Domain;
using MinimalKafka.Dashboard.Domain.Models;
using MinimalKafka.Runtime;

namespace MinimalKafka.Dashboard.Features;

internal static class GetTopicMessages
{
    internal const string Route = "/api/topics/messages";

    internal static async Task<IResult> HandleAsync(
        [FromServices] IKafkaDashboard dashboard,
        [FromServices] ITopicRegistry registry,
        [FromQuery] string topic,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        var messages = await dashboard.GetTopicMessagesAsync(topic, normalizedCount, cancellationToken)
            .WaitAsync(cancellationToken).ConfigureAwait(false);

        return Results.Ok(new Response(
            topic,
            normalizedCount,
            messages));
    }

    internal sealed record Response(string Topic, int RequestedCount, IReadOnlyList<TopicMessage> Messages)
    {
        public static Response Empty(string topic, int requestedCount) => new(topic, requestedCount, []);
    };


}
