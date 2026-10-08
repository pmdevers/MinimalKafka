using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinimalKafka.Producing;
using System.Text;

namespace MinimalKafka.Middleware.ClaimCheck;

/// <summary>
/// Producer middleware that stores large payloads in a claim-check store
/// and replaces the Kafka message value with a claim identifier header.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClaimCheckProducerMiddleware"/> class.
/// </remarks>
/// <param name="store">Claim-check storage used to persist payloads before publish.</param>
/// <param name="options">Configured claim-check middleware options.</param>
/// <param name="logger">Logger for middleware diagnostics.</param>
public sealed class ClaimCheckProducerMiddleware(
    IClaimCheckStore store,
    IOptions<ClaimCheckOptions> options,
    ILogger<ClaimCheckProducerMiddleware> logger) : IProducerMiddleware
{
    /// <inheritdoc/>
    public async Task InvokeAsync(ProducerContext context, ProducerDelegate next)
    {
        var settings = options.Value;
        if (context.Value.Length <= settings.ThresholdBytes)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var size = context.Value.Length;
        var claimId = await store.StoreAsync(context.Value, context.CancellationToken).ConfigureAwait(false);
        logger.LogDebug("Stored {Size} byte payload for {Topic} as claim {ClaimId}.", size, context.Topic, claimId);

        context.Headers.Remove(settings.HeaderName);
        context.Headers.Add(settings.HeaderName, Encoding.UTF8.GetBytes(claimId));
        context.Value = [];

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch
        {
            // The message was not published, so the stored payload would be orphaned.
            await store.DeleteAsync(claimId, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
