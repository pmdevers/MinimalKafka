using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Extension;

/// <summary>
/// Provides extension methods for producing messages using a <see cref="KafkaContext"/>.
/// </summary>
public static class KafkaContextExtensions
{
    /// <summary>
    /// Produces a message to the specified topic using the provided key and value.
    /// </summary>
    /// <typeparam name="TKey">The type of the message key.</typeparam>
    /// <typeparam name="TValue">The type of the message value.</typeparam>
    /// <param name="context">The <see cref="KafkaContext"/> used to resolve the producer and services.</param>
    /// <param name="topic">The name of the Kafka topic to produce to.</param>
    /// <param name="key">The key of the message.</param>
    /// <param name="value">The value of the message.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, with the delivery result.</returns>
    public static Task<DeliveryResult<TKey, TValue>> ProduceAsync<TKey, TValue>(this KafkaContext context, string topic, TKey key, TValue value)
        => context.Produce(topic, new Message<TKey, TValue>() { Key = key, Value = value });

    /// <summary>
    /// Produces a message to the specified topic using the provided <see cref="Message{TKey, TValue}"/> instance.
    /// </summary>
    /// <typeparam name="TKey">The type of the message key.</typeparam>
    /// <typeparam name="TValue">The type of the message value.</typeparam>
    /// <param name="context">The <see cref="KafkaContext"/> used to resolve the producer and services.</param>
    /// <param name="topic">The name of the Kafka topic to produce to.</param>
    /// <param name="message">The message to produce.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation, with the delivery result.</returns>
    public static async Task<DeliveryResult<TKey, TValue>> Produce<TKey, TValue>(this KafkaContext context, string topic, Message<TKey, TValue> message)
    {
        var producer = context.RequestServices.GetRequiredService<IProducer<TKey, TValue>>();
        return await producer.ProduceAsync(topic, message);
    }
}