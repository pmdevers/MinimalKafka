using MinimalKafka.Builders;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

internal class JoinBuilder<K1, V1, K2, V2>(IKafkaBuilder builder,
    ISourceBlock<Tuple<KafkaContext, K1, V1>> left, string topic) : IJoinBuilder<K1, V1, K2, V2>
{
    private readonly ISourceBlock<Tuple<KafkaContext, K1, V1>> _left = left;
    private readonly ISourceBlock<Tuple<KafkaContext, K2, V2>> _right = new ConsumeBlock<K2, V2>(builder, topic);

    public IIntoBuilder<TKey, Tuple<V1?, V2?>> On<TKey>(IStreamStore<TKey, Tuple<V1?, V2?>> store, Func<K1, V1, TKey> leftKey, Func<K2, V2, TKey> rightKey)
    {
        var leftKeyTransform = new KeyBlock<K1, V1, TKey>(leftKey);
        var rightKeyTransform = new KeyBlock<K2, V2, TKey>(rightKey);

        var storeLeft = new StoreBlock<TKey, V1, Tuple<V1?, V2?>>(store, 
            v => Tuple.Create<V1?, V2?>(v, default), 
            (o, v) => Tuple.Create<V1?, V2?>(v, o.Item2));

        var storeRight = new StoreBlock<TKey, V2, Tuple<V1?, V2?>>(store,
            v => Tuple.Create<V1?, V2?>(default, v),
            (o, v) => Tuple.Create<V1?, V2?>(o.Item1, v));

        _left.LinkTo(leftKeyTransform);
        _right.LinkTo(rightKeyTransform);

        leftKeyTransform.LinkTo(storeLeft);
        rightKeyTransform.LinkTo(storeRight);

        return new IntoBuilder<TKey, Tuple<V1?, V2?>>(storeLeft, storeRight);
    }

}

public class StoreBlock<TKey, TIn, TOut>(IStreamStore<TKey, TOut> store, Func<TIn, TOut> create, Func<TOut, TIn, TOut> update)
    : IPropagatorBlock<
        Tuple<KafkaContext, TKey, TIn>,
        Tuple<KafkaContext, TKey, TOut>>
{
    private readonly IPropagatorBlock<
        Tuple<KafkaContext, TKey, TIn>,
        Tuple<KafkaContext, TKey, TOut>> _transform = new TransformBlock<Tuple<KafkaContext, TKey, TIn>, Tuple<KafkaContext, TKey, TOut>>(data =>
        {
            var result = store.AddOrUpdate(data.Item2, _ => create(data.Item3), (_, v) => update(v, data.Item3));
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