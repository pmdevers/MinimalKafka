using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MinimalKafka.Hosting;

public static class MinimalKafkaExtensions
{
    /// <summary>Registers the Kafka consumer and producer and lets <paramref name="configure"/> customize them.</summary>
    public static IServiceCollection AddMinimalKafka(
        this IServiceCollection services,
        Action<IMinimalKafkaBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<KafkaConsumerOptions>();
        services.AddOptions<SerializationOptions>();
        services.TryAddSingleton<IMessageSerializerRegistry, MessageSerializerRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageSerializer, JsonMessageSerializer>());
        services.TryAddSingleton<TopicRegistry>();
        services.TryAddSingleton<IMessageProducer, KafkaMessageProducer>();
        if (!services.Any(descriptor => descriptor.ImplementationType == typeof(KafkaConsumerBackgroundService)))
        {
            services.AddHostedService<KafkaConsumerBackgroundService>();
        }

        configure?.Invoke(new MinimalKafkaBuilder(services));
        return services;
    }

    /// <summary>Configures the raw librdkafka settings.</summary>
    public static IMinimalKafkaBuilder WithConfiguration(
        this IMinimalKafkaBuilder builder,
        Action<KafkaConsumerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.Configure(configure);
        return builder;
    }

    /// <summary>Adds a global consumer middleware. Middleware runs in the order it is added.</summary>
    public static IMinimalKafkaBuilder UseConsumerMiddleware<TMiddleware>(this IMinimalKafkaBuilder builder)
        where TMiddleware : class, IConsumerMiddleware
    {
        builder.Services.AddScoped<IConsumerMiddleware, TMiddleware>();
        return builder;
    }

    /// <summary>Adds a producer middleware. Middleware runs in the order it is added.</summary>
    public static IMinimalKafkaBuilder UseProducerMiddleware<TMiddleware>(this IMinimalKafkaBuilder builder)
        where TMiddleware : class, IProducerMiddleware
    {
        builder.Services.AddScoped<IProducerMiddleware, TMiddleware>();
        return builder;
    }
}
