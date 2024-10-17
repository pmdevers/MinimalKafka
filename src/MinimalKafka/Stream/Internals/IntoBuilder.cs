using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

public class IntoBuilder<TKey, TValue>(
    ISourceBlock<Tuple<KafkaContext, TKey, TValue>> left,
    ISourceBlock<Tuple<KafkaContext, TKey, TValue>> right) : IIntoBuilder<TKey, TValue>
{
    private readonly ISourceBlock<Tuple<KafkaContext, TKey, TValue>> _left = left;
    private readonly ISourceBlock<Tuple<KafkaContext, TKey, TValue>> _right = right;

    public void Into(Func<KafkaContext, TKey, TValue, Task> handler)
    {
        var action = new IntoBlock<TKey, TValue>(handler);

        _left.LinkTo(action);
        _right.LinkTo(action);
    }
}


