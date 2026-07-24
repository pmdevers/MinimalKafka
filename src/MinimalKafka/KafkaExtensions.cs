using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MinimalKafka.Builders;
using MinimalKafka.Internals;
using MinimalKafka.Middlewares.DeadletterQueue;
using MinimalKafka.Serializers;

namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public static class KafkaExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddMinimalKafka(this IServiceCollection services, Action<IKafkaConfigBuilder>? config = null)
    {
        List<Action<IKafkaBuilder>> conventions = [];
        var configBuilder = new KafkaConfigConventionBuilder(services, conventions);


        configBuilder.WithClientId(Environment.MachineName);
        configBuilder.WithGroupId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithOffsetReset(AutoOffsetReset.Earliest);
        configBuilder.WithReportInterval(5);
        configBuilder.WithTopicFormatter(topic => topic);
        configBuilder.WithInMemoryStore();
        configBuilder.WithJsonSerializers();
        configBuilder.WithDeadLetterResolver(x => new InMemoryDeadLetterResolver());
        configBuilder.UpdateConfig(x =>
        {
            x.AddOrUpdate("enable.auto.commit", "false");
            x.AddOrUpdate("enable.auto.offset.store", "false");
        });


        config?.Invoke(configBuilder);

        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSingleton<IKafkaBuilder>(s =>
        {
            var b = new KafkaBuilder(s);
            conventions.ForEach(x => x(b));
            return b;
        });

        services.AddTransient(typeof(IKafkaSerializer<>), typeof(KafkaSerializerProxy<>));

        services.AddSingleton(sp =>
        {
            var builder = sp.GetRequiredService<IKafkaBuilder>();
            var config = builder.MetaData.ProducerConfig();
            var handlers = builder.MetaData.ProducerHandlers();

            return new ProducerBuilder<byte[], byte[]>(config)
                .SetLogHandler(handlers.LogHandler)
                .SetErrorHandler(handlers.ErrorHandler)
                .SetOAuthBearerTokenRefreshHandler(handlers.OAuthBearerTokenRefreshHandler)
                .SetStatisticsHandler(handlers.StatisticsHandler)
                .SetKeySerializer(Confluent.Kafka.Serializers.ByteArray)
                .SetValueSerializer(Confluent.Kafka.Serializers.ByteArray)
                .Build();
        });

        services.AddSingleton<IKafkaProducer, KafkaContextProducer>();
        services.AddHostedService<KafkaService>();
        return services;
    }

    /// <summary>
    /// Maps a Kafka topic to a specified handler within the application.
    /// </summary>
    /// <remarks>This method integrates Kafka topic handling into the application's pipeline by associating a
    /// specific topic with a message handler. The handler is invoked for each message received on the specified
    /// topic.</remarks>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> used to configure the application.</param>
    /// <param name="topic">The name of the Kafka topic to map. Cannot be null or empty.</param>
    /// <param name="handler">The delegate that processes messages from the specified topic.  The delegate must match the expected signature
    /// for handling Kafka messages.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> that can be used to further configure the Kafka topic mapping.</returns>
    public static IKafkaConventionBuilder MapTopic(this IApplicationBuilder builder, string topic, Delegate handler)
    {
        var tb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();
        return tb.MapTopic(topic, handler);
    }

    /// <summary>
    /// Maps a Kafka topic to a specified handler for processing messages.
    /// </summary>
    /// <remarks>This method associates a Kafka topic with a handler delegate, enabling message processing for
    /// the specified topic. The returned <see cref="IKafkaConventionBuilder"/> can be used to define additional
    /// conventions or metadata for the topic.</remarks>
    /// <param name="builder">The <see cref="IKafkaBuilder"/> used to configure Kafka topics and handlers.</param>
    /// <param name="topic">The name of the Kafka topic to map. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="handler">A delegate that processes messages from the specified topic. The delegate must match the expected signature for
    /// handling Kafka messages.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> that allows further configuration of Kafka conventions.</returns>
    public static IKafkaConventionBuilder MapTopic(this IKafkaBuilder builder, string topic, Delegate handler)
    {
        return builder.GetOrAddDatasource()
            .AddTopicDelegate(topic, handler)
            .WithMetaData([.. builder.MetaData]);
    }

    private static IKafkaDataSource GetOrAddDatasource(this IKafkaBuilder builder)
    {
        builder.DataSource ??= new KafkaDataSource(builder.ServiceProvider);
        return builder.DataSource;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <param name="create"></param>
    /// <returns></returns>
    public static TBuilder WithStoreFactory<TBuilder>(this TBuilder builder, Func<IServiceProvider, IKafkaStoreFactory> create)
        where TBuilder : IKafkaConfigBuilder
    {
        builder.Services.AddSingleton(sp => create(sp));
        return builder;
    }

    /// <summary>
    /// Configures the builder to use an in-memory store for Kafka-related operations.
    /// </summary>
    /// <remarks>This method sets up an in-memory store, which is useful for testing or scenarios where
    /// persistence is not required. It overrides the default store factory with an in-memory implementation.</remarks>
    /// <typeparam name="TBuilder">The type of the builder implementing <see cref="IKafkaConfigBuilder"/>.</typeparam>
    /// <param name="builder">The builder instance to configure.</param>
    /// <param name="timeProvider"></param>
    /// <returns>The configured builder instance.</returns>
    public static TBuilder WithInMemoryStore<TBuilder>(this TBuilder builder, TimeProvider? timeProvider = null)
        where TBuilder : IKafkaConfigBuilder
    {
        builder.Services.AddSingleton(sp => new KafkaInMemoryStoreFactory(sp, timeProvider ?? TimeProvider.System));
        builder.Services.AddHostedService(sp => sp.GetRequiredService<KafkaInMemoryStoreFactory>());
        builder.WithStoreFactory(sp => sp.GetRequiredService<KafkaInMemoryStoreFactory>());
        return builder;
    }

    /// <summary>
    /// Configures the builder to use a specified dead letter resolver for Kafka-related operations.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the builder implementing <see cref="IKafkaConfigBuilder"/>.</typeparam>
    /// <typeparam name="TResolver">The type of the dead letter resolver implementing <see cref="IDeadLetterResolver"/>.</typeparam>
    /// <param name="builder">The builder instance to configure.</param>
    /// <param name="implementationFactory">A factory function to create the dead letter resolver instance.</param>
    /// <returns>The configured builder instance.</returns>
    public static TBuilder WithDeadLetterResolver<TBuilder, TResolver>(this TBuilder builder, Func<IServiceProvider, TResolver>? implementationFactory = null)
        where TBuilder : IKafkaConfigBuilder
        where TResolver : class, IDeadLetterResolver
    {
        if (implementationFactory == null)
            builder.Services.AddSingleton<TResolver>();
        else
            builder.Services.AddSingleton(implementationFactory);

        builder.Services.AddSingleton<IDeadLetterResolver>(sp => sp.GetRequiredService<TResolver>());
        return builder;
    }
}