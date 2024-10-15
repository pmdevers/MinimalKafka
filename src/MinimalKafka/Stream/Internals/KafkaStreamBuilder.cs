using Confluent.Kafka;
using MinimalKafka.Builders;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

public class KafkaStreamBuilder<K, V> : IStreamBuilder<K, V>
{
    private readonly IKafkaBuilder _builder;
    private readonly BufferBlock<Tuple<KafkaContext, K, V>> _source;

    public KafkaStreamBuilder(IKafkaBuilder builder, string topic)
    {
        _builder = builder;
        _source = new BufferBlock<Tuple<KafkaContext, K, V>>();

        _builder.MapTopic(topic, async (KafkaContext context, K key, V value) =>
        {
            await _source.SendAsync(Tuple.Create(context, key, value));
            await _source.Completion;
        });

    }

    public void Into(string topic)
    {
        var action = new ActionBlock<Tuple<KafkaContext, K, V>>(async data =>
        {
            await data.Item1.Produce(topic, new Message<K, V>()
            {
                Key = data.Item2,
                Value = data.Item3
            });
        });

        _source.LinkTo(action);
    }

    public void Into(Func<KafkaContext, K, V, Task> handler)
    {
        var action = new ActionBlock<Tuple<KafkaContext, K, V>>(async data =>
        {
            await handler.Invoke(data.Item1, data.Item2, data.Item3);
        });

        _source.LinkTo(action);
    }

    public IJoinBuilder<K, V, K2, V2> Join<K2, V2>(string topic)
    {
        return new JoinBuilder<K, V, K2, V2>(_builder, _source, topic);
    }
}

