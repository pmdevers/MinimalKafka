using MinimalKafka.Builders;
using MinimalKafka.Extension;
using MinimalKafka.Stream.Internals;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Blocks;

public class ConsumeBlock<TKey, TValue> :
    ILinkTo<(KafkaContext, TKey, TValue)>
{
    private readonly BufferBlock<(KafkaContext, TKey, TValue)> _target;
    private readonly IKafkaConventionBuilder _builder;

    public IKafkaConventionBuilder Builder => _builder;

    public Task Completion => _target.Completion;

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

    public void LinkTo(ITargetBlock<(KafkaContext, TKey, TValue)> target, DataflowLinkOptions options)
    {
        _target.LinkTo(_target, options);
    }

    public void Fault(Exception exception)
    {
        (_target as IDataflowBlock).Fault(exception);
    }
    public void Complete()
    {
        _target.Complete();
    }
}
