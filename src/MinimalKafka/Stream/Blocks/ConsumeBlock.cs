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

    public ConsumeBlock(IKafkaBuilder builder, string topic)
    {
        _target = new BufferBlock<(KafkaContext, TKey, TValue)>();

        _builder = builder.MapTopic(topic, async (KafkaContext context, TKey key, TValue value) =>
        {
            try
            {
                await _target.SendAsync((context, key, value));
            }
            catch (Exception error)
            {
                ((IDataflowBlock) _target).Fault(error);
            }
        });
    }

    public void LinkTo(ITargetBlock<(KafkaContext, TKey, TValue)> target, DataflowLinkOptions options)
    {
        _target.LinkTo(target, options);
    }
}
