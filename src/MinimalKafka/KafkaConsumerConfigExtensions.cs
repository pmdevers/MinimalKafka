using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalKafka.Internals;
using MinimalKafka.Metadata;
using MinimalKafka.Metadata.Internals;
using MinimalKafka.Serializers;
using System.Text.Json;

namespace MinimalKafka;

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
    public static TBuilder WithTopicFormatter<TBuilder>(this TBuilder builder, KafkaTopicFormatter topicFormatter)
        where TBuilder : IKafkaConfigBuilder
    {
        builder.Services.RemoveAll<KafkaTopicFormatter>();
        builder.Services.AddSingleton(topicFormatter);
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
    /// <param name="offsetReset"></param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithOffsetReset<TBuilder>(this TBuilder builder, AutoOffsetReset offsetReset)
         where TBuilder : IKafkaConventionBuilder
    {
        return builder.UpdateConsumerConfig(x => x.AutoOffsetReset = offsetReset);
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

    /// <summary>
    /// Ensures that a metadata object of type <typeparamref name="TMetadata"/> exists in the builder's metadata collection,
    /// and applies the specified assignment action to each instance.
    /// </summary>
    /// <typeparam name="TMetadata">The type of metadata to ensure and assign.</typeparam>
    /// <param name="b">The Kafka builder.</param>
    /// <param name="assign">The action to apply to each metadata instance.</param>
    public static void Ensure<TMetadata>(this IKafkaBuilder b, Action<TMetadata> assign)
        where TMetadata : new()
    {
        if (!b.MetaData.OfType<TMetadata>().Any())
            b.MetaData.Add(new TMetadata());

        foreach (var ch in b.MetaData.OfType<TMetadata>())
            assign(ch);
    }

    /// <summary>
    /// Configures the specified <see cref="IKafkaConfigBuilder"/> to use JSON-based serializers and deserializers for
    /// Kafka message keys and values.
    /// </summary>
    /// <remarks>This method registers JSON serializers and deserializers for both keys and values in Kafka
    /// messages. The serializers use the <see cref="JsonSerializerOptions"/> provided via the <paramref
    /// name="options"/> parameter, or default to <see cref="JsonSerializerDefaults.Web"/> if no options are
    /// specified.</remarks>
    /// <param name="builder">The <see cref="IKafkaConfigBuilder"/> to configure.</param>
    /// <param name="options">An optional action to configure the <see cref="JsonSerializerOptions"/> used by the JSON serializers. If not
    /// provided, the default options for <see cref="JsonSerializerDefaults.Web"/> will be used.</param>
    /// <returns>The configured <see cref="IKafkaConfigBuilder"/> instance, allowing for further chaining of configuration methods.</returns>
    public static IKafkaConfigBuilder WithJsonSerializers(this IKafkaConfigBuilder builder, Action<JsonSerializerOptions>? options = null)
    {
        var defaults = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options?.Invoke(defaults);

        builder.Services.AddSingleton<ISerializerFactory>(new SystemTextJsonSerializerFactory(defaults));

        return builder;
    }

    /// <summary>
    /// Registers a handler for the partition assigned event on the consumer.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="handler">The handler to invoke when partitions are assigned.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithPartitionAssignedHandler<TBuilder>(this TBuilder builder, Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.PartitionsAssignedHandler = handler));
        return builder;
    }

    /// <summary>
    /// Registers a handler for the partition revoked event on the consumer.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="handler">The handler to invoke when partitions are revoked.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithPartitionRevokedHandler<TBuilder>(this TBuilder builder, Action<object, List<TopicPartitionOffset>> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.PartitionsRevokedHandler = handler));
        return builder;
    }

    /// <summary>
    /// Registers a handler for the partition lost event on the consumer.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="handler">The handler to invoke when partitions are lost.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithPartitionLostHandler<TBuilder>(this TBuilder builder, Func<object, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.PartitionsLostHandler = handler));
        return builder;
    }

    /// <summary>
    /// Registers an error handler for the consumer.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="handler">The error handler delegate.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithErrorHandler<TBuilder>(this TBuilder builder, Action<object, Error> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.ErrorHandler = handler));
        return builder;
    }

    /// <summary>
    /// Registers a statistics handler for the consumer.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="handler">The statistics handler delegate.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithStatisticsHandler<TBuilder>(this TBuilder builder, Action<object, string> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.StatisticsHandler = handler));
        return builder;
    }

    /// <summary>
    /// Registers a log handler for the consumer.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="handler">The log handler delegate.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithLogHandler<TBuilder>(this TBuilder builder, Action<object, LogMessage> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.LogHandler = handler));
        return builder;
    }

    /// <summary>
    /// Adds report interval metadata to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="reportInterval">The report interval in milliseconds.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithReportInterval<TBuilder>(this TBuilder builder, int reportInterval)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new ReportIntervalMetadataAttribute(reportInterval));
        return builder;
    }
}
