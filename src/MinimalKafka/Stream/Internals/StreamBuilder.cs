using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;

namespace MinimalKafka.Stream.Internals;

internal class StreamBuilder<TKey, TValue>(IKafkaBuilder builder, string topic) : IStreamBuilder<TKey, TValue>
{
    private readonly IKafkaBuilder _builder = builder;
    private readonly ConsumeBlock<TKey, TValue> _source = new(builder, topic);

    public void Add(Action<IKafkaBuilder> convention)
    {
        _source.Builder.Add(convention);
    }

    public void Finally(Action<IKafkaBuilder> finalConvention)
    {
        _source.Builder.Finally(finalConvention);
    }

    public IJoinBuilder<TKey, TValue, K2, V2> Join<K2, V2>(string topic)
    {
        return new JoinBuilder<TKey, TValue, K2, V2>(_builder, _source, topic);
    }

    public IKafkaConventionBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler)
    {
        return new IntoBuilder<TKey, TValue>(_source.Builder, _source).Into(handler);
    }
}
