using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Runtime;

namespace MinimalKafka.Dashboard.Features;

internal static class StopConsumers
{
    internal const string Route = "/api/consumer/stop";

    internal static Task<IResult> HandleAsync(
        [FromServices] IConsumerRuntimeControl control,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        control.Stop();
        return Task.FromResult(Results.Json(new { control.IsRunning }));
    }
}
