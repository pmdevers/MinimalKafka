using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Runtime;

namespace MinimalKafka.Dashboard.Features;

internal static class StartConsumers
{
    internal const string Route = "/api/consumer/start";

    internal static Task<IResult> HandleAsync(
        [FromServices] IConsumerRuntimeControl control,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        control.Start();
        return Task.FromResult(Results.Json(new { control.IsRunning }));
    }
}
