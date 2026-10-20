using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Serialization;
using System.Text;

namespace MinimalKafka.Producing;

internal sealed class KafkaMessageProducer(
    IKafkaProducerFactory producerFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaMessageProducer> logger,
    IKafkaSerializerRegistry serializers,
    ITopicNamingConvention topicNameConvention,
    IKafkaCorrelationContextAccessor correlationContextAccessor
    ) : IKafkaProducer, IDisposable
{
    private readonly IProducer<byte[], byte[]> _producer = producerFactory.Create();

    public async Task<DeliveryResult<byte[], byte[]>> ProduceAsync<TKey, TValue>(
        string topic,
        TKey key,
        TValue value,
        Headers? headers = null,
        CancellationToken cancellationToken = default,
        string? format = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        var topicName = topicNameConvention.Apply(topic);
        var messageHeaders = headers ?? [];

        if (!CorrelationHeaders.ContainsCorrelationId(messageHeaders))
        {
            var inheritedCorrelationId = correlationContextAccessor.CurrentCorrelationId;
            var correlationId = string.IsNullOrWhiteSpace(inheritedCorrelationId)
                ? Guid.NewGuid().ToString("D")
                : inheritedCorrelationId;

            CorrelationHeaders.SetCorrelationId(messageHeaders, correlationId);
        }

        // Raw bytes are sent as-is. Other values use the topic format; the schema subject is derived from the physical topic name.
        var keyPayload = key switch
        {
            byte[] bytes => bytes,
            string text => Encoding.UTF8.GetBytes(text),
            _ => await serializers.Get(format)
                .SerializeAsync(key, topicName, messageHeaders, cancellationToken)
                .ConfigureAwait(false)
        };

        var payload = value switch
        {
            byte[] bytes => bytes,
            string text => Encoding.UTF8.GetBytes(text),
            _ => await serializers.Get(format)
                .SerializeAsync(value, topicName, messageHeaders, cancellationToken)
                .ConfigureAwait(false)
        };

        var context = new ProducerContext(topicName, keyPayload, payload, messageHeaders, cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var middleware = scope.ServiceProvider.GetServices<IProducerMiddleware>().ToArray();
        ProducerDelegate pipeline = PublishAsync;
        for (var index = middleware.Length - 1; index >= 0; index--)
        {
            var current = middleware[index];
            var next = pipeline;
            pipeline = producerContext => current.InvokeAsync(producerContext, next);
        }

        await pipeline(context).ConfigureAwait(false);
        return context.DeliveryResult
            ?? throw new InvalidOperationException("Producer middleware completed without publishing the message.");
    }

    private async Task PublishAsync(ProducerContext context)
    {
        context.DeliveryResult = await _producer.ProduceAsync(
            context.Topic,
            new Message<byte[], byte[]>
            {
                Key = context.Key,
                Value = context.Value,
                Headers = context.Headers
            },
            context.CancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Produced Kafka message to {Topic} partition {Partition} at offset {Offset}.",
                context.DeliveryResult.Topic,
                context.DeliveryResult.Partition,
                context.DeliveryResult.Offset);
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}




