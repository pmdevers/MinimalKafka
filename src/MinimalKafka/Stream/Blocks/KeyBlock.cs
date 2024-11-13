using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Blocks;

public class KeyBlock<TKey, TValue, TResult>(Func<TKey, TValue, TResult> keySelector) :
    IPropagatorBlock<
        Tuple<KafkaContext, TKey, TValue>,
        Tuple<KafkaContext, TResult, TValue>>
{
    private readonly IPropagatorBlock<
        Tuple<KafkaContext, TKey, TValue>,
        Tuple<KafkaContext, TResult, TValue>> _target = new TransformBlock<
            Tuple<KafkaContext, TKey, TValue>,
            Tuple<KafkaContext, TResult, TValue>>
        (dat =>
        {
            var result = keySelector(dat.Item2, dat.Item3);
            return Tuple.Create(dat.Item1, result, dat.Item3);
        });

    public Task Completion => _target.Completion;

    public void Complete()
    {
        _target.Complete();
    }

    public Tuple<KafkaContext, TResult, TValue>? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TResult, TValue>> target, out bool messageConsumed)
    {
        return _target.ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public void Fault(Exception exception)
    {
        _target?.Fault(exception);
    }

    public IDisposable LinkTo(ITargetBlock<Tuple<KafkaContext, TResult, TValue>> target, DataflowLinkOptions linkOptions)
    {
        return _target.LinkTo(target, linkOptions);
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Tuple<KafkaContext, TKey, TValue> messageValue, ISourceBlock<Tuple<KafkaContext, TKey, TValue>>? source, bool consumeToAccept)
    {
        return _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }

    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TResult, TValue>> target)
    {
        _target.ReleaseReservation(messageHeader, target);
    }

    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TResult, TValue>> target)
    {
        return _target.ReserveMessage(messageHeader, target);
    }
}