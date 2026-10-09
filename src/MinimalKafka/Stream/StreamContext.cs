using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Producing;

namespace MinimalKafka.Stream;

/// <summary>
/// Stream-processing wrapper around <see cref="KafkaContext"/>.
/// </summary>
public sealed class StreamContext(KafkaContext context)
{
    /// <summary>The underlying Kafka context.</summary>
    public KafkaContext KafkaContext { get; } = context;

    /// <summary>The current topic name.</summary>
    public string TopicName => KafkaContext.Topic;

    /// <summary>The current message headers.</summary>
    public Headers Headers => KafkaContext.Headers;

    /// <summary>The scoped service provider for the current message.</summary>
    public IServiceProvider RequestServices => KafkaContext.RequestServices;

    /// <summary>The Kafka producer available for publishing additional messages.</summary>
    public IKafkaProducer Producer => KafkaContext.Producer;

    /// <summary>The cancellation token for the current message.</summary>
    public CancellationToken CancellationToken => KafkaContext.CancellationToken;

    /// <summary>Produces a Kafka message using the configured producer.</summary>
    public Task<DeliveryResult<byte[], byte[]>> ProduceAsync<TKey, TValue>(
        string topic,
        TKey key,
        TValue value,
        Headers? headers = null,
        CancellationToken cancellationToken = default,
        string? format = null)
    {
        var effectiveCancellationToken = cancellationToken == default ? CancellationToken : cancellationToken;
        return Producer.ProduceAsync(topic, key, value, headers, effectiveCancellationToken, format);
    }

    /// <summary>Gets the stream store for the specified logical topic.</summary>
    public IKafkaStore GetTopicStore(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var topicName = RequestServices.GetRequiredService<ITopicNamingConvention>().Apply(topic);
        return RequestServices.GetRequiredService<IKafkaStoreFactory>().GetStore(topicName);
    }
}
