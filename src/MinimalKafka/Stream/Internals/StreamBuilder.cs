using MinimalKafka.Builders;

namespace MinimalKafka.Stream.Internals;

internal sealed class StreamBuilder<TKey, TValue>(IKafkaBuilder builder, string topic) 
    : IStreamBuilder<TKey, TValue>
    where TKey : notnull
{
    private readonly string _topic = topic;

    public IJoinBuilder<TKey, TValue, K2, V2> Join<K2, V2>(string topic)
        where K2 : notnull
    {
        return new JoinBuilder<TKey, TValue, K2, V2>(builder, _topic, topic, false);
    }

    public IJoinBuilder<TKey, TValue, K2, V2> InnerJoin<K2, V2>(string topic)
        where K2 : notnull
    {
        return new JoinBuilder<TKey, TValue, K2, V2>(builder, _topic, topic, true);
    }

    public IKafkaConventionBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler)
        => builder.MapTopic(_topic, (KafkaContext c, TKey key, TValue value) => handler(c, key, value));
}
