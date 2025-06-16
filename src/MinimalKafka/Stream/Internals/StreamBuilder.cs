using MinimalKafka.Builders;

namespace MinimalKafka.Stream.Internals;

internal sealed class StreamBuilder<TKey, TValue>(IKafkaBuilder builder, string topic) 
    : IStreamBuilder<TKey, TValue>
{
    private readonly string _topic = topic;

    public IJoinBuilder<TKey, TValue, K2, V2> Join<K2, V2>(string topic)
    {
        return new JoinBuilder<TKey, TValue, K2, V2>(builder, _topic, topic);
    }

    public IKafkaConventionBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler)
        => builder.MapTopic(_topic, (KafkaContext c, TKey key, TValue value) => handler(c, key, value));
}
