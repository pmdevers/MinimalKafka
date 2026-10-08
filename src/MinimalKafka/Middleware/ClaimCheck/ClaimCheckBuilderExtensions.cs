using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalKafka.Middleware.ClaimCheck;

namespace MinimalKafka;

/// <summary>
/// Extension methods for registering claim check middleware in a MinimalKafka pipeline.
/// </summary>
public static class ClaimCheckBuilderExtensions
{
    extension(IMinimalKafkaBuilder builder)
    {
        /// <summary>
        /// Adds the claim check consumer and producer middleware. Call it before other middleware
        /// so a claim is resolved before any other consumer middleware or the handler sees the message.
        /// Register a custom <see cref="IClaimCheckStore"/> beforehand to replace the file system store.
        /// </summary>
        public IMinimalKafkaBuilder WithClaimCheck(
            Action<ClaimCheckOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var options = builder.Services.AddOptions<ClaimCheckOptions>();
            if (configure is not null)
            {
                options.Configure(configure);
            }
            options.Validate(
                value => value.ThresholdBytes >= 0 && !string.IsNullOrWhiteSpace(value.HeaderName),
                "ClaimCheckOptions requires a non-negative ThresholdBytes and a HeaderName.");

            builder.Services.TryAddSingleton<IClaimCheckStore, FileSystemClaimCheckStore>();
            return builder
                .UseConsumerMiddleware<ClaimCheckConsumerMiddleware>()
                .UseProducerMiddleware<ClaimCheckProducerMiddleware>();
        }
    }


}
