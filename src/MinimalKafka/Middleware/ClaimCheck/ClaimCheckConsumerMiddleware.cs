using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace MinimalKafka.Middleware.ClaimCheck;

/// <summary>
/// Consumer middleware that resolves claim-check references from message headers
/// and replaces the message value with the stored payload.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClaimCheckConsumerMiddleware"/> class.
/// </remarks>
/// <param name="store">Claim-check storage used to retrieve and optionally delete payloads.</param>
/// <param name="options">Configured claim-check middleware options.</param>
/// <param name="logger">Logger for middleware diagnostics.</param>
public sealed class ClaimCheckConsumerMiddleware(
    IClaimCheckStore store,
    IOptions<ClaimCheckOptions> options,
    ILogger<ClaimCheckConsumerMiddleware> logger) : IConsumerMiddleware
{
    /// <inheritdoc/>
    public async Task InvokeAsync(KafkaContext context, ConsumerDelegate next)
    {
        var settings = options.Value;
        var header = context.Headers.LastOrDefault(item => item.Key == settings.HeaderName);
        var headerValue = header?.GetValueBytes();
        if (headerValue is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var claimId = Encoding.UTF8.GetString(headerValue);
        var payload = await store.RetrieveAsync(claimId, context.CancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Claim '{claimId}' for topic '{context.Topic}' was not found in the claim check store.");

        logger.LogDebug("Resolved claim {ClaimId} ({Size} bytes) for {Topic}.", claimId, payload.Length, context.Topic);
        context.Value = payload;

        await next(context).ConfigureAwait(false);

        if (settings.DeleteAfterConsume)
        {
            await store.DeleteAsync(claimId, context.CancellationToken).ConfigureAwait(false);
        }
    }
}
