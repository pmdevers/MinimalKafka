using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;
using System.Text.RegularExpressions;

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

}
