using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serializers;

namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public static class KafkaContextExtenions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="topicName"></param>
    /// <returns></returns>
    public static IKafkaStore GetTopicStore(this KafkaContext context, string topicName)
    {
        return context.GetStoreFactory()
            .GetStore(context.ConsumerKey with
        {
            TopicName = topicName
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static IKafkaStore GetTopicStore(this KafkaContext context)
    {
        return context.GetStoreFactory()
            .GetStore(context.ConsumerKey);
    }

    private static IKafkaStoreFactory GetStoreFactory(this KafkaContext context)
        => context.RequestServices.GetRequiredService<IKafkaStoreFactory>();

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="context"></param>
    /// <param name="topic"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="headers"></param>
    public static void Produce<TKey, TValue>(this KafkaContext context, string topic, TKey key, TValue value, Dictionary<string, string>? headers = null)
    {
        var keySerializer = context.RequestServices.GetRequiredService<IKafkaSerializer<TKey>>();
        var valueSerializer = context.RequestServices.GetRequiredService<IKafkaSerializer<TValue>>();

        context.Produce(new()
        {
            Topic = topic,
            Key = keySerializer.Serialize(key),
            Value = valueSerializer.Serialize(value),
            Headers = headers ?? []
        });

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="context"></param>
    /// <param name="topic"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public static Task ProduceAsync<TKey, TValue>(this KafkaContext context, string topic, TKey key, TValue value, Dictionary<string, string>? headers = null)
    {
        context.Produce(topic, key, value, headers);
        return Task.CompletedTask;
    }
}
