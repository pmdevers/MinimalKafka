using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;
using System.Reflection.Emit;

namespace MinimalKafka.Extension;
public static class KafkaConsumerConfigMetadataExtensions
{
    public static void Ensure<TMetadata>(this IKafkaBuilder b, Action<TMetadata> assign)
        where TMetadata : new()
    {
        if (!b.MetaData.OfType<TMetadata>().Any())
            b.MetaData.Add(new TMetadata());

        foreach (var ch in b.MetaData.OfType<TMetadata>())
            assign(ch);
    }

    public static TBuilder WithConfiguration<TBuilder>(this TBuilder builder, IConfiguration configuration)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(ConfigurationMetadata.FromConfig(configuration));
        return builder;
    }

    public static TBuilder WithGroupId<TBuilder>(this TBuilder builder, string groupId)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new GroupIdMetadata(groupId));
        return builder;
    }

    public static TBuilder WithClientId<TBuilder>(this TBuilder builder, string clientId)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new ClientIdMetadataAttribute(clientId));
        return builder;
    }

    public static TBuilder WithBootstrapServers<TBuilder>(this TBuilder builder, string bootstrapServers)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new BootstrapServersMetadata(bootstrapServers));
        return builder;
    }

    public static TBuilder WithOffsetReset<TBuilder>(this TBuilder builder, AutoOffsetReset autoOffsetReset)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new AutoOffsetResetMetadata(autoOffsetReset));
        return builder;
    }

    public static TBuilder WithReportInterval<TBuilder>(this TBuilder builder, int reportInterval)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new ReportIntervalMetadata(reportInterval));
        return builder;
    }

    public static TBuilder WithTopicFormatter<TBuilder>(this TBuilder builder, Func<string, string> topicFormatter)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new TopicFormatterMetadataAttribute(topicFormatter));
        return builder;
    }

    public static TBuilder WithClientIdFormatter<TBuilder>(this TBuilder builder, Func<string, string> clientIdFormatter)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new TopicFormatterMetadataAttribute(clientIdFormatter));
        return builder;
    }

    public static TBuilder WithPartitionAssignedHandler<TBuilder>(this TBuilder builder, Func<object, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.PartitionsAssignedHandler = handler));
        return builder;
    }

    public static TBuilder WithPartitionRevokedHandler<TBuilder>(this TBuilder builder, Action<object, List<TopicPartitionOffset>> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.PartitionsRevokedHandler = handler));
        return builder;
    }

    public static TBuilder WithPartitionLostHandler<TBuilder>(this TBuilder builder, Func<object, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.PartitionsLostHandler = handler));
        return builder;
    }

    public static TBuilder WithErrorHandler<TBuilder>(this TBuilder builder, Action<object, Error> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.ErrorHandler = handler));
        return builder;
    }

    public static TBuilder WithStatisticsHandler<TBuilder>(this TBuilder builder, Action<object, string> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.StatisticsHandler = handler));
        return builder;
    }

    public static TBuilder WithLogHandler<TBuilder>(this TBuilder builder, Action<object, LogMessage> handler)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.Add(b => b.Ensure<ConsumerHandlerMetadata>(ch => ch.LogHandler = handler));
        return builder;
    }

    public static TBuilder WithAutoCommit<TBuilder>(this TBuilder builder, bool enabled = true)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new AutoCommitMetaDataAttribute(enabled));
        return builder;
    }
}
