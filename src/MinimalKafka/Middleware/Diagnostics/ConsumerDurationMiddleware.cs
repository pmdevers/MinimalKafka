using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MinimalKafka.Middleware.Diagnostics;

/// <summary>Logs how long a consumed message took to process.</summary>
internal sealed class ConsumerDurationMiddleware(ILogger<ConsumerDurationMiddleware> logger) : IConsumerMiddleware
{
    public async Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Consumed message from {Topic} in {Elapsed}.",
                context.Topic,
                stopwatch.Elapsed);
        }
    }
}
