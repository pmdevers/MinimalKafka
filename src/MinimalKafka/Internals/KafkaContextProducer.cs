using Confluent.Kafka;
using System.Text;

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
        if (!ctx.Messages.Any())
            return;

        foreach (var msg in ctx.Messages)
        {
            var formmattedTopic = formatter(msg.Topic);

            try
            {
                var headers = new Headers();

                foreach (var item in msg.Headers)
                {
                    headers.Add(item.Key, Encoding.UTF8.GetBytes(item.Value));
                }

                await producer.ProduceAsync(formmattedTopic, new Message<byte[], byte[]>()
                {
                    Key = msg.Key,
                    Value = msg.Value,
                    Headers = headers,
                }, ct);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }

        }
    }

    public async Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Dictionary<string, string>? header = null)
    {
        var consumerKey = KafkaConsumerKey.Random(topic);
        using var context = KafkaContext.Create(consumerKey, serviceProvider);
        await context.ProduceAsync(topic, key, value, header);
        await ProduceAsync(context, CancellationToken.None);
    }
}