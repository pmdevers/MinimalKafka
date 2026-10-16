using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Runtime;

namespace MinimalKafka.Dashboard.Features;

internal static class GetConsumerStatus
{
    internal const string Route = "/api/consumer/status";

    internal static Task<IResult> HandleAsync(
        [FromServices] IConsumerRuntimeControl control,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Results.Json(new { control.IsRunning }));
    }
}
