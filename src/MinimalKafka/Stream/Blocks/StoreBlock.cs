using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Blocks;

public class StoreBlock<TKey, TIn, TOut>(IStreamStore<TKey, TOut> store, Func<TIn, TOut> create, Func<TOut, TIn, TOut> update)
    : IPropagatorBlock<
        Tuple<KafkaContext, TKey, TIn>,
        Tuple<KafkaContext, TKey, TOut>>
{
    private readonly IPropagatorBlock<
        Tuple<KafkaContext, TKey, TIn>,
        Tuple<KafkaContext, TKey, TOut>> _transform = new TransformBlock<Tuple<KafkaContext, TKey, TIn>, Tuple<KafkaContext, TKey, TOut>>(async data =>
        {
            var result = await store.AddOrUpdate(data.Item2, _ => create(data.Item3), (_, v) => update(v, data.Item3));
            return Tuple.Create(data.Item1, data.Item2, result);
        });

    public Task Completion => _transform.Completion;

    public void Complete()
    {
        _transform.Complete();
    }

    public Tuple<KafkaContext, TKey, TOut>? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TKey, TOut>> target, out bool messageConsumed)
    {
        return _transform.ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public void Fault(Exception exception)
    {
        _transform.Fault(exception);
    }

    public IDisposable LinkTo(ITargetBlock<Tuple<KafkaContext, TKey, TOut>> target, DataflowLinkOptions linkOptions)
    {
        return _transform.LinkTo(target, linkOptions);
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Tuple<KafkaContext, TKey, TIn> messageValue, ISourceBlock<Tuple<KafkaContext, TKey, TIn>>? source, bool consumeToAccept)
    {
        return _transform.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TKey, TOut>> target)
    {
        _transform.ReleaseReservation(messageHeader, target);
    }

    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TKey, TOut>> target)
    {
        return _transform.ReserveMessage(messageHeader, target);
    }
}
