using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Runtime;

namespace MinimalKafka.Stream;

/// <summary>
/// Extension methods for mapping stream-processing topologies.
/// </summary>
public static class KafkaStreamApplicationBuilderExtensions
{
    /// <summary>
    /// Maps a Kafka topic as a strongly typed stream source.
    /// </summary>
    public static StreamSourceBuilder<TKey, TValue> MapStream<TKey, TValue>(this IApplicationBuilder builder, string topic)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var topicName = builder.ApplicationServices.GetRequiredService<ITopicNamingConvention>().Apply(topic);
        var registry = builder.ApplicationServices.GetRequiredService<TopicRegistry>();
        return new StreamSourceBuilder<TKey, TValue>(builder, registry, topicName);
    }
}
