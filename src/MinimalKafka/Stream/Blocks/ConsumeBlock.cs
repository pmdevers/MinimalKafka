using MinimalKafka.Builders;
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
        var options = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1, // Enables multiple tasks
            EnsureOrdered = false       // Prevents the whole block from stopping on one failure
        };

        var buffer = new BufferBlock<(KafkaContext, TKey, TValue)>(options);

        _builder = builder.MapTopic(topic, async (KafkaContext context, TKey key, TValue value) =>
        {
            
             await buffer.SendAsync((context, key, value));

            await buffer.Completion;
            
        });

        _target = buffer;
    }

    public void LinkTo(ITargetBlock<(KafkaContext, TKey, TValue)> target, DataflowLinkOptions options)
    {
        _target.LinkTo(target, options);
    }
}
