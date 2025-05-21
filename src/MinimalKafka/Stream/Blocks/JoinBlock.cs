using MinimalKafka.Stream.Internals;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Blocks;

public class JoinBlock<K1, V1, K2, V2> : ILinkTo<(KafkaContext, (V1, V2))>
{
    private readonly IStreamStore<K1, V1> _leftStore;
    private readonly IStreamStore<K2, V2> _rightStore;
    private readonly Func<V1, V2, bool> _on;
    private readonly TransformManyBlock<(KafkaContext, K1, V1), (KafkaContext, (V1, V2))> _leftTransform;
    private readonly TransformManyBlock<(KafkaContext, K2, V2), (KafkaContext, (V1, V2))> _rightTransform;

    public JoinBlock(
        IStreamStore<K1, V1> leftStore, 
        IStreamStore<K2, V2> rightStore, 
        Func<V1, V2, bool> on
    )
    {
        _leftStore = leftStore;
        _rightStore = rightStore;
        _on = on;
        _leftTransform = new TransformManyBlock<(KafkaContext, K1, V1), (KafkaContext, (V1, V2))>(TransformLeft);
        _rightTransform = new TransformManyBlock<(KafkaContext, K2, V2), (KafkaContext, (V1, V2))>(TransformRight);
    }

    public ITargetBlock<(KafkaContext, K1, V1)> Left => _leftTransform;
    public ITargetBlock<(KafkaContext, K2, V2)> Right => _rightTransform;

    private ITargetBlock<(KafkaContext, (V1, V2))>? _target = null;

    public Task Completion => Task.CompletedTask;

    public void Complete()
    {
        
    }

    public void Fault(Exception exception)
    {
        _target?.Fault(exception);
    }

    public void LinkTo(ITargetBlock<(KafkaContext, (V1, V2))> target, DataflowLinkOptions options)
    {
        _target = target;
        _leftTransform.LinkTo(target, options);
        _rightTransform.LinkTo(target, options);
    }

    private async IAsyncEnumerable<(KafkaContext, (V1, V2))> TransformLeft((KafkaContext, K1, V1) data)
    {
        var (context, key, value) = data;
        var left = await _leftStore.AddOrUpdate(key, (k) => value, (k, v) => value);
        await foreach (var right in _rightStore.FindAsync(x => _on(left, x)))
        {
            yield return (context, (left, right));
        }
    }

    private async IAsyncEnumerable<(KafkaContext, (V1, V2))> TransformRight((KafkaContext, K2, V2) data)
    {
        var (context, key, value) = data;
        var right = await _rightStore.AddOrUpdate(key, (k) => value, (k, v) => value);
        await foreach ( var left in _leftStore.FindAsync(x => _on(x, right))) 
        {
            yield return (context, (left, right));
        }
    }
}
