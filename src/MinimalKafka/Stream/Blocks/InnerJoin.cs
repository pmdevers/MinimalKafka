using MinimalKafka.Stream.Internals;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Blocks;
public class InnerJoinBlock<TKey, K1, V1, K2, V2> : ILinkTo<(KafkaContext, TKey, (V1?, V2?))>
{
    private readonly IStreamStore<TKey, (V1?, V2?)> _store;
    private readonly Func<K1, V1, TKey> _leftKeySelector;
    private readonly Func<K2, V2, TKey> _rightKeySelector;
    private readonly TransformBlock<(KafkaContext, K1, V1), (KafkaContext, TKey, (V1?, V2?))> _leftTransform;
    private readonly TransformBlock<(KafkaContext, K2, V2), (KafkaContext, TKey, (V1?, V2?))> _rightTransform;

    public InnerJoinBlock(
        IStreamStore<TKey, (V1?, V2?)> store,
        Func<K1, V1, TKey> leftKeySelector,
        Func<K2, V2, TKey> rightKeySelector
    )
    {
        _store = store;
        _leftKeySelector = leftKeySelector;
        _rightKeySelector = rightKeySelector;
        _leftTransform = new(TransformLeft);
        _rightTransform = new(TransformRight);
    }

    public ITargetBlock<(KafkaContext, K1, V1)> Left => _leftTransform;
    public ITargetBlock<(KafkaContext, K2, V2)> Right => _rightTransform;

    public void LinkTo(ITargetBlock<(KafkaContext, TKey, (V1?, V2?))> block, DataflowLinkOptions options)
    {
        _leftTransform.LinkTo(block, options);
        _rightTransform.LinkTo(block, options);
    }

    private async Task<(KafkaContext, TKey, (V1?, V2?))> TransformLeft((KafkaContext, K1, V1) data)
    {
        var (context, key, value) = data;
        var storeKey = _leftKeySelector(key, value);
        var result = await _store.AddOrUpdate(storeKey, (k) => new(value, default), (k, v) => new(value, v.Item2));
        return (context, storeKey, result);
    }

    private async Task<(KafkaContext, TKey, (V1?, V2?))> TransformRight((KafkaContext, K2, V2) data)
    {
        var (context, key, value) = data;
        var storeKey = _rightKeySelector(key, value);
        var result = await _store.AddOrUpdate(storeKey, (k) => new(default, value), (k, v) => new(v.Item1, value));
        return (context, storeKey, result);
    }
}

