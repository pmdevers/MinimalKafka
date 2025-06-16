using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka;

public static class KafkaContextExtensions
{
    public static Task<DeliveryResult<TKey, TValue>> ProduceAsync<TKey, TValue>(this KafkaContext context, string topic, TKey key, TValue value)
        => Produce(context, topic, new Message<TKey, TValue>() { Key = key, Value = value });

    public static async Task<DeliveryResult<TKey, TValue>> Produce<TKey, TValue>(this KafkaContext context, string topic, Message<TKey, TValue> message)
    {
        var producer = context.RequestServices.GetRequiredService<IProducer<TKey, TValue>>();
        return await producer.ProduceAsync(topic, message);
    }
}