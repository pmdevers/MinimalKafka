using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

public interface ILinkTo<out TOutput>
{
    void LinkTo(ITargetBlock<TOutput> block, DataflowLinkOptions options);
}


internal class IntoBuilder<TValue>(
    IKafkaConventionBuilder conventionBuilder,
    ILinkTo<(KafkaContext, TValue)> source) : IIntoBuilder<TValue>
{
    public IKafkaConventionBuilder Into(Func<KafkaContext, TValue, Task> handler)
    {
        var action = new IntoBlock<TValue>(handler);

        source.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });

        return conventionBuilder;
    }
}

internal class IntoBuilder<TKey, TValue>(
    IKafkaConventionBuilder conventionBuilder, 
    ILinkTo<(KafkaContext, TKey, TValue)> consumeBlock) : IIntoBuilder<TKey, TValue>
{
    public IKafkaConventionBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler)
    {
        var action = new ActionBlock<(KafkaContext, TKey, TValue)>(async data =>
        {
            var (context, key, value) = data;
            await handler(context, key, value);
        });

        consumeBlock.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });

        return conventionBuilder;
    }
}