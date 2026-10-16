using Microsoft.Extensions.Logging;

namespace MinimalKafka.Middleware.Resilience;

internal sealed class RetryMiddleware(RetryOptions options, ILogger<RetryMiddleware> logger) : IConsumerMiddleware
{
    public async Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
    {
        var delay = options.Delay;
        for (int attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            try
            {
                await next(context).ConfigureAwait(false);
                return;
            }
            catch (Exception exception)
            {
                // Check if we should retry this exception
                if (context.CancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(exception, "Operation for {Topic} cancelled at attempt {Attempt}.", context.Topic, attempt);
                    throw;
                }

                var shouldRetry = options.ShouldRetry?.Invoke(exception) ?? true;
                if (!shouldRetry)
                {
                    logger.LogError(exception, "Non-retryable exception for {Topic} at attempt {Attempt}.", context.Topic, attempt);
                    throw;
                }

                if (attempt >= options.MaxAttempts)
                {
                    logger.LogError(
                        exception,
                        "All {MaxAttempts} attempts exhausted for {Topic}. Last attempt failed.",
                        options.MaxAttempts, context.Topic);
                    throw;
                }

                logger.LogWarning(
                    exception,
                    "Attempt {Attempt}/{MaxAttempts} for {Topic} failed; retrying in {Delay}.",
                    attempt, options.MaxAttempts, context.Topic, delay);

                await Task.Delay(delay, context.CancellationToken).ConfigureAwait(false);

                delay = TimeSpan.FromTicks(Math.Min(
                    options.MaxDelay.Ticks,
                    (long)(delay.Ticks * options.BackoffMultiplier)));
            }
        }
    }
}
