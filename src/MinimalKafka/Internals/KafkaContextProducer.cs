using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace MinimalKafka.Internals;

/// <summary>
/// 
/// </summary>
public delegate string KafkaTopicFormatter(string topic);

internal class KafkaContextProducer : IKafkaProducer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaTopicFormatter _formatter;
    private readonly ILogger<KafkaContextProducer> _logger;

    public KafkaContextProducer(
        IServiceProvider serviceProvider,
        KafkaTopicFormatter formatter,
        ILogger<KafkaContextProducer> logger)
    {
        _serviceProvider = serviceProvider;
        _formatter = formatter;
        _logger = logger;
    }

    public async Task ProduceAsync(KafkaContext ctx, CancellationToken ct)
    {
        if(!ctx.Messages.Any())
            return;

        var config = ctx.Metadata.ProducerConfig();
        var producer = new ProducerBuilder<byte[], byte[]>(config)
            .SetKeySerializer(Confluent.Kafka.Serializers.ByteArray)
            .SetValueSerializer(Confluent.Kafka.Serializers.ByteArray)
            .Build();

        producer.InitTransactions(TimeSpan.FromSeconds(5));

        try
        {
            producer.BeginTransaction();

            foreach (var msg in ctx.Messages)
            {
                var formmattedTopic = _formatter(msg.Topic);
                await producer.ProduceAsync(formmattedTopic, new Message<byte[], byte[]>()
                {
                    Key = msg.Key,
                    Value = msg.Value
                }, ct);
            }

            producer.CommitTransaction();
        }
        catch (KafkaException ex) when (ex.Error.IsFatal)
        {
            _logger.LogCritical(ex, ex.Message);
            producer.AbortTransaction();
            throw new InvalidOperationException("Producer fenced/invalid", ex);
        }
        catch
        {
            producer.AbortTransaction();
            throw;
        }
        finally
        {
            producer.Dispose();
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