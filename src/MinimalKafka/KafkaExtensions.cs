using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Internals;
using MinimalKafka.Metadata;
using MinimalKafka.Metadata.Internals;
using MinimalKafka.Serializers;
using System.Reflection.Emit;

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

        configBuilder.WithClientId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithGroupId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithTransactionalId(AppDomain.CurrentDomain.FriendlyName);
        configBuilder.WithTopicFormatter(s => s);

        config?.Invoke(configBuilder);

        services.AddSingleton<IKafkaBuilder>(s =>
        {
            var b = new KafkaBuilder(s);
            conventions.ForEach(x => x(b));
            return b;
        });

        services.AddSingleton<IKafkaStoreFactory>(sp => new KafkaInMemoryStoreFactory(sp));
        services.AddTransient(typeof(IKafkaSerializer<>), typeof(SystemTextJsonSerializer<>));

        services.AddSingleton<IKafkaProducer>(sp =>
        {
            var builder = sp.GetRequiredService<IKafkaBuilder>();
            var config = builder.MetaData.OfType<IConfigMetadata>().First();
            var producer = new ProducerBuilder<byte[], byte[]>(config.ProducerConfig.AsEnumerable())
                .SetKeySerializer(Confluent.Kafka.Serializers.ByteArray)
                .SetValueSerializer(Confluent.Kafka.Serializers.ByteArray)
                .Build();
            return new KafkaContextProducer(producer);
        });

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
    public static  IKafkaConventionBuilder MapTopic(this IKafkaBuilder builder, string topic, Delegate handler)
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
}

/// <summary>
/// 
/// </summary>
public static class KafkaConsumerConfigExtensions
{
    /// <summary>
    /// Adds configuration metadata from the specified <see cref="IConfiguration"/> to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="configuration">The configuration source.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithConfiguration<TBuilder>(this TBuilder builder, IConfiguration configuration)
         where TBuilder : IKafkaConventionBuilder
    {
        return builder.WithSingle(ConfigMetadataAttribute.FromConfig(configuration));
    }

    /// <summary>
    /// Adds client ID metadata to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="clientId">The client ID.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithClientId<TBuilder>(this TBuilder builder, string clientId)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.UpdateConsumerConfig(builder => builder.ClientId = clientId);
        return builder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public static TBuilder WithGroupId<TBuilder>(this TBuilder builder, string groupId)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.UpdateConsumerConfig(x => x.GroupId = groupId);
        return builder;
    }

    /// <summary>
    /// Adds a topic name formatter to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="topicFormatter">A function to format topic names.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithTopicFormatter<TBuilder>(this TBuilder builder, Func<string, string> topicFormatter)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new TopicFormatterMetadataAttribute(topicFormatter));
        return builder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <param name="bootstrapServers"></param>
    /// <returns></returns>
    public static TBuilder WithBootstrapServers<TBuilder>(this TBuilder builder, string bootstrapServers)
        where TBuilder : IKafkaConventionBuilder
    {
        return builder.UpdateConsumerConfig(x => x.BootstrapServers = bootstrapServers);
    }

    /// <summary>
    /// Adds auto offset reset metadata to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithOffsetReset<TBuilder>(this TBuilder builder)
         where TBuilder : IKafkaConventionBuilder
    {
        return builder.UpdateConsumerConfig(x => x.AutoOffsetReset = AutoOffsetReset.Earliest);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <param name="transactionalId"></param>
    /// <returns></returns>
    public static TBuilder WithTransactionalId<TBuilder>(this TBuilder builder, string transactionalId)
         where TBuilder : IKafkaConventionBuilder
    {
        return builder.UpdateProducerConfig(x => x.TransactionalId = transactionalId);
    }

    internal static TBuilder UpdateConsumerConfig<TBuilder>(this TBuilder builder, Action<ConsumerConfig> update)
        where TBuilder: IKafkaConventionBuilder
    {
        builder.Add(b => { 
            var item = b.MetaData.OfType<IConfigMetadata>().FirstOrDefault();
            if(item is null)
            {
                item = new ConfigMetadataAttribute(new Dictionary<string, string>());
                b.MetaData.Add(item);
            }

            update?.Invoke(item.ConsumerConfig);
        });
        return builder;
    }

    internal static TBuilder UpdateProducerConfig<TBuilder>(this TBuilder builder, Action<ProducerConfig> update)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => {
            var item = b.MetaData.OfType<IConfigMetadata>().FirstOrDefault();
            if (item is null)
            {
                item = new ConfigMetadataAttribute(new Dictionary<string, string>());
                b.MetaData.Add(item);
            }

            update?.Invoke(item.ProducerConfig);
        });
        return builder;
    }

    /// <summary>
    /// Adds one or more metadata items to the builder's metadata collection.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="items">The metadata items to add.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithMetaData<TBuilder>(this TBuilder builder, params object[] items)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b =>
        {
            foreach (var item in items)
            {
                b.MetaData.Add(item);
            }
        });

        return builder;
    }

    /// <summary>
    /// Removes any existing metadata of the same type as the provided item, then adds the new metadata item.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="metadata">The metadata item to add (after removing existing items of the same type).</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithSingle<TBuilder>(this TBuilder builder, object metadata)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.RemoveMetaData(metadata);
        builder.WithMetaData(metadata);
        return builder;
    }

    /// <summary>
    /// Removes all metadata items of the same type as the provided item from the builder's metadata collection.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="item">The metadata item whose type will be used for removal.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder RemoveMetaData<TBuilder>(this TBuilder builder, object item)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b =>
        {
            b.MetaData.RemoveAll(x => x.GetType() == item.GetType());
        });

        return builder;
    }
}
