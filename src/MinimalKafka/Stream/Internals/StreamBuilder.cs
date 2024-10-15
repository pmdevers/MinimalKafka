using Confluent.Kafka;
using MinimalKafka.Builders;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

public class StreamBuilder<TKey, TValue> : IStreamBuilder<TKey, TValue>
{
    private readonly IKafkaBuilder _builder;
    private readonly BufferBlock<Tuple<KafkaContext, TKey, TValue>> _source;

    public StreamBuilder(IKafkaBuilder builder, string topic)
    {
        _builder = builder;
        _source = new BufferBlock<Tuple<KafkaContext, TKey, TValue>>();

        _builder.MapTopic(topic, async (KafkaContext context, TKey key, TValue value) =>
        {
            await _source.SendAsync(Tuple.Create(context, key, value));
            await _source.Completion;
        });

    }

    public void Into(string topic)
    {
        var action = new ActionBlock<Tuple<KafkaContext, TKey, TValue>>(async data =>
        {
            await data.Item1.Produce(topic, new Message<TKey, TValue>()
            {
                Key = data.Item2,
                Value = data.Item3
            });
        });

        _source.LinkTo(action);
    }

    public void Into(Func<KafkaContext, TKey, TValue, Task> handler)
    {
        var action = new ActionBlock<Tuple<KafkaContext, TKey, TValue>>(async data =>
        {
            await handler.Invoke(data.Item1, data.Item2, data.Item3);
        });

        _source.LinkTo(action);
    }

    public IJoinBuilder<TKey, TValue, K2, V2> Join<K2, V2>(string topic)
    {
        return new JoinBuilder<TKey, TValue, K2, V2>(_builder, _source, topic);
    }
}

