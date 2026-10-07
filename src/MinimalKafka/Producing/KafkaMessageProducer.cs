using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalKafka.Producing;

internal sealed class KafkaMessageProducer : IMessageProducer, IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaMessageProducer> _logger;
    private readonly IMessageSerializerRegistry _serializers;
    private readonly ITopicNamingConvention? _naming;

    public KafkaMessageProducer(
        IOptions<KafkaConsumerOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaMessageProducer> logger,
        IMessageSerializerRegistry serializers,
        IEnumerable<ITopicNamingConvention> naming)
    {
        _serializers = serializers;
        _naming = naming.LastOrDefault();
        _producer = new ProducerBuilder<string, byte[]>(options.Value.CreateProducerConfig())
            .SetValueSerializer(Serializers.ByteArray)
            .Build();
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<DeliveryResult<string, byte[]>> ProduceAsync<TValue>(
        string topic,
        TValue value,
        string? key = null,
        Headers? headers = null,
        CancellationToken cancellationToken = default,
        string? format = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        var messageHeaders = headers ?? [];
        // Raw bytes are sent as-is. Other values use the topic format; the schema subject is derived from the physical topic name.
        var payload = value as byte[]
            ?? await _serializers.Get(format)
                .SerializeAsync(value, _naming?.Apply(topic) ?? topic, messageHeaders, cancellationToken)
                .ConfigureAwait(false);
        var context = new ProducerContext(topic, key, payload, messageHeaders, cancellationToken);

        await using var scope = _scopeFactory.CreateAsyncScope();
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
            new Message<string, byte[]>
            {
                Key = context.Key!,
                Value = context.Value,
                Headers = context.Headers
            },
            context.CancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Produced Kafka message to {Topic} partition {Partition} at offset {Offset}.",
            context.DeliveryResult.Topic,
            context.DeliveryResult.Partition,
            context.DeliveryResult.Offset);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}




