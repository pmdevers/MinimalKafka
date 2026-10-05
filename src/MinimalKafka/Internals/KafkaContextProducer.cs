using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serializers;
using System.Text;

namespace MinimalKafka.Internals;

/// <summary>
/// 
/// </summary>
public delegate string KafkaTopicFormatter(string topic);

/// <summary>
/// 
/// </summary>
/// <param name="serviceProvider"></param>
/// <param name="producer"></param>
public class KafkaProducer(IServiceProvider serviceProvider, IProducer<byte[], byte[]> producer)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task Invoke(ProduceMessage message)
    {
        var headers = new Headers();

        foreach (var item in message.Headers)
        {
            headers.Add(item.Key, Encoding.UTF8.GetBytes(item.Value));
        }

        await producer.ProduceAsync(message.Topic, new Message<byte[], byte[]>()
        {
            Key = Serialize(message.Key),
            Value = Serialize(message.Value),
            Headers = headers,
        });
    }

    private byte[] Serialize(object? value)
    {
        if (value == null)
        {
            return [];
        }

        var serializerType = typeof(IKafkaSerializer<>).MakeGenericType(value.GetType());
        var serializer = serviceProvider.GetRequiredService(serializerType);

        var result = serializerType.InvokeMember(
            nameof(IKafkaSerializer<>.Serialize),
            System.Reflection.BindingFlags.Public,
            Type.DefaultBinder,
            serializer,
            [value]);

        return result is null ? [] : (byte[])result;
    }
}

internal class DiKafkaProducer(
    IServiceProvider serviceProvider,
    KafkaMiddlewareProducer producer) : IKafkaProducer
{
    public async Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Dictionary<string, string>? header = null)
    {
        var consumerKey = KafkaConsumerKey.Random(topic);
        using var context = KafkaContext.Create(consumerKey, serviceProvider);
        await context.ProduceAsync(topic, key, value, header);
        await producer.Produce(context);
    }
}