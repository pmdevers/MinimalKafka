using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalKafka.Middlewares.DeadletterQueue;

/// <summary>
/// Provides extension methods for adding the DeadLetterQueueMiddleware to the Kafka processing pipeline.
/// </summary>
public static class DeadLetterQueueMiddlewareExtensions
{
    /// <summary>
    /// Adds the DeadLetterQueueMiddleware to the Kafka processing pipeline.
    /// </summary>
    /// <param name="builder">The Kafka builder.</param>
    /// <param name="configure">The action to configure the DeadLetterQueueOptions.</param>
    /// <returns>The updated Kafka builder.</returns>
    public static TBuilder WithDeadLetterQueue<TBuilder>(this TBuilder builder, Action<DeadLetterQueueOptions>? configure = null)
        where TBuilder : IKafkaConventionBuilder
    {
        if (builder is IKafkaConfigBuilder configBuilder)
        {
            configBuilder.Services.Configure(configure ?? (_ => { }));
            configBuilder.Use<DeadLetterQueueMiddleware>();

            return builder;
        }

        builder.Use(sp =>
        {
            var options = new DeadLetterQueueOptions();
            configure?.Invoke(options);

            var resolver = sp.GetRequiredService<IDeadLetterResolver>();
            var option = Options.Create(options);
            var logger = sp.GetRequiredService<ILogger<DeadLetterQueueMiddleware>>();

            return new DeadLetterQueueMiddleware(resolver, option, logger).InvokeAsync;
        });

        return builder;
    }
}