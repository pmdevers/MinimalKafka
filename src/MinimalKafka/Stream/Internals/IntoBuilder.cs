using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

public class IntoBuilder<TKey, TValue>(
    IWithMetadataBuilder streamBuilder,
    ISourceBlock<Tuple<KafkaContext, TKey, TValue>> left,
    ISourceBlock<Tuple<KafkaContext, TKey, TValue>> right) : IIntoBuilder<TKey, TValue>
{
    private readonly IWithMetadataBuilder _streamBuilder = streamBuilder;
    private readonly ISourceBlock<Tuple<KafkaContext, TKey, TValue>> _left = left;
    private readonly ISourceBlock<Tuple<KafkaContext, TKey, TValue>> _right = right;

    public IWithMetadataBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler)
    {
        var action = new IntoBlock<TKey, TValue>(handler);

        _left.LinkTo(action);
        _right.LinkTo(action);

        return _streamBuilder;
    }
}


