using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using MinimalKafka.Builders;
using MinimalKafka.Metadata.Internals;

namespace MinimalKafka.Extension;

/// <summary>
/// Provides extension methods for configuring consumer-related metadata and handlers on Kafka builders.
/// </summary>
public static class KafkaConsumerConfigMetadataExtensions
{
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
    /// Adds configuration metadata from the specified <see cref="IConfiguration"/> to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="configuration">The configuration source.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithConfiguration<TBuilder>(this TBuilder builder, IConfiguration configuration)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(ConfigurationMetadata.FromConfig(configuration));
        return builder;
    }

    /// <summary>
    /// Adds group ID metadata to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="groupId">The consumer group ID.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithGroupId<TBuilder>(this TBuilder builder, string groupId)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new GroupIdMetadata(groupId));
        return builder;
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
        builder.WithSingle(new ClientIdMetadataAttribute(clientId));
        return builder;
    }

    /// <summary>
    /// Adds bootstrap servers metadata to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="bootstrapServers">The bootstrap servers string.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithBootstrapServers<TBuilder>(this TBuilder builder, string bootstrapServers)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new BootstrapServersMetadata(bootstrapServers));
        return builder;
    }

    /// <summary>
    /// Adds auto offset reset metadata to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="autoOffsetReset">The auto offset reset policy.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithOffsetReset<TBuilder>(this TBuilder builder, AutoOffsetReset autoOffsetReset)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new AutoOffsetResetMetadata(autoOffsetReset));
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
        builder.WithSingle(new ReportIntervalMetadata(reportInterval));
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
    /// Adds a client ID formatter to the builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="clientIdFormatter">A function to format client IDs.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithClientIdFormatter<TBuilder>(this TBuilder builder, Func<string, string> clientIdFormatter)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new TopicFormatterMetadataAttribute(clientIdFormatter));
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
    /// Adds auto-commit metadata to the builder, enabling or disabling automatic offset commits.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="enabled">Whether auto-commit should be enabled. Defaults to <c>true</c>.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithAutoCommit<TBuilder>(this TBuilder builder, bool enabled = true)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new AutoCommitMetaDataAttribute(enabled));
        return builder;
    }
}
