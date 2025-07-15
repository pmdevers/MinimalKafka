using Confluent.Kafka;

namespace MinimalKafka.Internals;

/// <summary>
/// 
/// </summary>
public delegate string KafkaTopicFormatter(string topic);

internal class KafkaContextProducer(
    IServiceProvider serviceProvider,
    IProducer<byte[], byte[]> producer,
    KafkaTopicFormatter formatter) : IKafkaProducer
{
    public async Task ProduceAsync(KafkaContext ctx, CancellationToken ct)
    {
        if(!ctx.Messages.Any())
            return;
                
        foreach (var msg in ctx.Messages)
        {
            var formmattedTopic = formatter(msg.Topic);

            try
            {
                await producer.ProduceAsync(formmattedTopic, new Message<byte[], byte[]>()
                {
                    Key = msg.Key,
                    Value = msg.Value
                }, ct);

            } catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

        }
    }

    public async Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Dictionary<string, string>? header = null)
    {
        var context = KafkaContext.Create(topic, [], new() { Key = [], Value = [] }, serviceProvider);
        await context.ProduceAsync(topic, key, value, header);
        await ProduceAsync(context, CancellationToken.None);
    }
}