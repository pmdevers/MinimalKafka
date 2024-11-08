using MinimalKafka.Builders;
using MinimalKafka.Extension;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Blocks;

public class ConsumeBlock<TKey, TValue> :
    ISourceBlock<(KafkaContext, TKey, TValue)>
{
    private readonly ISourceBlock<(KafkaContext, TKey, TValue)> _target;
    private readonly IKafkaConventionBuilder _builder;

    public IKafkaConventionBuilder Builder => _builder;

    public ConsumeBlock(IKafkaBuilder builder, string topic)
    {
        var buffer = new BufferBlock<(KafkaContext, TKey, TValue)>();

        _builder = builder.MapTopic(topic, async (KafkaContext context, TKey key, TValue value) =>
        {
            await buffer.SendAsync((context, key, value));
            await buffer.Completion;
        });

        _target = buffer;
    }

    public Task Completion => _target.Completion;

    public void Complete()
    {
        _target.Complete();
    }

    public (KafkaContext, TKey, TValue) ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<(KafkaContext, TKey, TValue)> target, out bool messageConsumed)
    {
        return _target.ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public void Fault(Exception exception)
    {
        _target.Fault(exception);
    }

    public IDisposable LinkTo(ITargetBlock<(KafkaContext, TKey, TValue)> target, DataflowLinkOptions linkOptions)
    {
        return _target.LinkTo(target, linkOptions);
    }

    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<(KafkaContext, TKey, TValue)> target)
    {
        _target.ReleaseReservation(messageHeader, target);
    }

    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<(KafkaContext, TKey, TValue)> target)
    {
        return _target.ReserveMessage(messageHeader, target);
    }
}
