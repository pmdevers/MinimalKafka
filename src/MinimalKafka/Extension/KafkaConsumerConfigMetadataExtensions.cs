﻿using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;

namespace MinimalKafka.Extension;
public static class KafkaConsumerConfigMetadataExtensions
{
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

}
