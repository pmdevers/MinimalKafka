using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Metadata;
using MinimalKafka.Serializers;

namespace MinimalKafka.Internals;

internal class KafkaContextProducer : IKafkaProducer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProducer<byte[], byte[]> _producer;

    public KafkaContextProducer(IServiceProvider serviceProvider, IProducer<byte[], byte[]> producer)
    {
        _serviceProvider = serviceProvider;
        _producer = producer;
        _producer.InitTransactions(TimeSpan.FromSeconds(5));
    }
    public async Task ProduceAsync(KafkaContext context, CancellationToken cancellationToken)
    {
        if(!context.Messages.Any())
            return;

        try
        {
            var topicFormatter = context.Metadata.OfType<ITopicFormaterMetadata>().FirstOrDefault();
            
            _producer.BeginTransaction();
            foreach (var produce in context.Messages)
            {
                var message = produce with
                {
                    Topic = topicFormatter?.TopicFormatter.Invoke(produce.Topic) ?? produce.Topic
                };

                await ProduceAsync(message, cancellationToken);
            }

            _producer.CommitTransaction();
        }
        catch (ProduceException<byte[], byte[]> ex)
        {
            _producer.AbortTransaction();
            throw new KafkaProcesException(ex, ex.Message);
        }
    }

    public async Task ProduceAsync(KafkaMessage message, CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(message.Topic, new()
        {
                Key = message.Key,
                Value = message.Value,
                Headers = message.GetKafkaHeaders()
        }, cancellationToken);
    }

    public Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Dictionary<string, string>? headers = null, CancellationToken token = default)
    {
        var keySerializer = _serviceProvider.GetRequiredService<IKafkaSerializer<TKey>>();
        var valueSerializer = _serviceProvider.GetRequiredService<IKafkaSerializer<TValue>>();
        var timeprovider = _serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

        return ProduceAsync(new KafkaMessage()
        {
            Topic = topic,
            Key = keySerializer.Serialize(key),
            Value = valueSerializer.Serialize(value),
            Headers = headers ?? [],
            Timestamp = timeprovider.GetTimestamp()
        }, token);
    }
}

    