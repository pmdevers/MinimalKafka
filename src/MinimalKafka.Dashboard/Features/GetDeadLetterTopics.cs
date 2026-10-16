using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Runtime;

namespace MinimalKafka.Dashboard.Features;

internal static class GetDeadLetterTopics
{
    internal const string Route = "/api/dead-letter/topics";

    internal static IResult HandleAsync(
        [FromServices] ITopicRegistry registry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mappings = registry.GetDeadLetterTopics()
            .Select(entry => new Mapping(entry.Key, entry.Value))
            .OrderBy(entry => entry.SourceTopic, StringComparer.Ordinal)
            .ToArray();

        return Results.Ok(new Response(mappings));
    }

    internal sealed record Response(IReadOnlyList<Mapping> Topics);

    internal sealed record Mapping(string SourceTopic, string DeadLetterTopic);
}
