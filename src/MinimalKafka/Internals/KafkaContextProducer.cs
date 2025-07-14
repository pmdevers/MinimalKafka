using Confluent.Kafka;

namespace MinimalKafka.Internals;

/// <summary>
/// 
/// </summary>
public delegate string KafkaTopicFormatter(string topic);

internal class KafkaContextProducer : IKafkaProducer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProducer<byte[], byte[]> _producer;
    private readonly KafkaTopicFormatter _formatter;

    public KafkaContextProducer(
        IServiceProvider serviceProvider,
        IProducer<byte[], byte[]> producer,
        KafkaTopicFormatter formatter)
    {
        _serviceProvider = serviceProvider;
        _producer = producer;
        _formatter = formatter;

        _producer.InitTransactions(TimeSpan.FromSeconds(5));
    }
    public async Task ProduceAsync(KafkaContext ctx, CancellationToken ct)
    {
        if(!ctx.Messages.Any())
            return;

        try
        {
            _producer.BeginTransaction();
            foreach (var msg in ctx.Messages)
            {
                var formmattedTopic = _formatter(msg.Topic);
                await _producer.ProduceAsync(formmattedTopic, new Message<byte[], byte[]>()
                {
                    Key = msg.Key,
                    Value = msg.Value
                }, ct);
            }

            _producer.CommitTransaction();
        }
        catch (KafkaException ex) when (ex.Error.IsFatal)
        {
            _producer.AbortTransaction();
            throw new InvalidOperationException("Producer fenced/invalid", ex);
        }
        catch
        {
            _producer.AbortTransaction();
            throw;
        }
    }

    public async Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Dictionary<string, string>? header = null)
    {
        var config = KafkaConsumerConfig.Create(KafkaConsumerKey.Random(Guid.NewGuid().ToString()), [], []);
        var context = KafkaContext.Create(config, new() { Key = [], Value = [] }, _serviceProvider);
        await context.ProduceAsync(topic, key, value, header);
        await ProduceAsync(context, CancellationToken.None);
    }
}