using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

public class IntoBuilder<K1, V1, K2, V2> : IIntoBuilder<(V1, V2)>
{
    private readonly IKafkaConventionBuilder _conventionBuilder;
    private readonly JoinBlock<K1, V1, K2, V2> _source;

    public IntoBuilder(
        IKafkaConventionBuilder conventionBuilder,
        JoinBlock<K1, V1, K2, V2> source)
    {
        _conventionBuilder = conventionBuilder;
        _source = source;
    }

    public IKafkaConventionBuilder Into(Func<KafkaContext, (V1, V2), Task> handler)
    {
        var action = new IntoBlock<(V1, V2)>(handler);

        _source.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });

        return _conventionBuilder;
    }
}

public class IntoBuilder<K1, V1> : IIntoBuilder<K1,V1>
{
    private readonly ConsumeBlock<K1, V1> _consumeBlock;

    public IntoBuilder(ConsumeBlock<K1, V1> consumeBlock)
    {
        _consumeBlock = consumeBlock;
    }
    public IKafkaConventionBuilder Into(Func<KafkaContext, K1, V1, Task> handler)
    {
        var action = new ActionBlock<(KafkaContext, K1, V1)>(async data =>
        {
            var (context, key, value) = data;
            await handler(context, key, value);
        });

        _consumeBlock.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });

        return _consumeBlock.Builder;
    }
}