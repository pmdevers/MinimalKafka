using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Builders.Internals;
using MinimalKafka.Extension;
using MinimalKafka.Serializers;

namespace MinimalKafka;

/// <summary>
/// Provides extension methods for configuring and using MinimalKafka.
/// </summary>
public static class KafkaExtensions
{
    /// <summary>
    /// Configures and registers Kafka-related services with the specified settings.
    /// </summary>
    /// <remarks>This method sets up minimal Kafka integration by registering essential services such as
    /// producers,  a hosted Kafka service, and a builder for further customization. By default, it configures the
    /// client ID  and group ID to match the application's domain name, disables auto-commit, and uses JSON
    /// serializers.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Kafka services will be added.</param>
    /// <param name="config">A delegate to configure the Kafka settings using an <see cref="IAddKafkaBuilder"/>.  This allows customization
    /// of client ID, group ID, serializers, topic formatting, and other Kafka-related options.</param>
    /// <summary>
    /// Registers MinimalKafka services and configuration into the dependency injection container, enabling Kafka producers, consumers, and topic handlers for the application.
    /// </summary>
    /// <param name="config">A delegate to configure Kafka options and conventions using the provided builder.</param>
    /// <returns>The <see cref="IServiceCollection"/> with the Kafka services registered.</returns>
    public static IServiceCollection AddMinimalKafka(this IServiceCollection services, Action<IAddKafkaBuilder> config)
    {
        List<Action<IKafkaBuilder>> conventions = [];
        var configBuilder = new AddKafkaBuilder(services, conventions);

        configBuilder.WithClientId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithGroupId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithAutoCommit(false);
        configBuilder.WithJsonSerializers();
        configBuilder.WithTopicFormatter(topic => topic);
        configBuilder.WithClientId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithGroupId(AppDomain.CurrentDomain.FriendlyName);

        config(configBuilder);

        services.AddSingleton<IKafkaBuilder>(s =>
        {
            var b = new KafkaBuilder(s);
            conventions.ForEach(x => x(b));
            return b;
        });
        services.AddHostedService<KafkaService>();

        services.AddSingleton(typeof(IProducer<,>), typeof(KafkaProducerFactory<,>));

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
        return builder
            .GetOrAddTopicDataSource()
            .AddTopicDelegate(topic, handler)
            .WithMetaData([.. builder.MetaData]);
    }

    private static IKafkaDataSource GetOrAddTopicDataSource(this IKafkaBuilder builder)
    {
        builder.DataSource ??= new KafkaDataSource(builder.ServiceProvider);
        return builder.DataSource;
    }
}