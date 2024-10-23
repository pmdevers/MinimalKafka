using MinimalKafka.Builders;
using MinimalKafka.Extension;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

public class StreamBuilder<TKey, TValue>(IKafkaBuilder builder, string topic) : IStreamBuilder<TKey, TValue>
{
    private readonly IKafkaBuilder _builder = builder;
    private readonly ConsumeBlock<TKey, TValue> _source = new(builder, topic);

    public IWithMetadataBuilder Into(string topic)
    {
        var action = new IntoBlock<TKey, TValue>(topic);
        _source.LinkTo(action);
        return this;
    }

    public IWithMetadataBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler)
    {
        var action = new IntoBlock<TKey, TValue>(handler);
        _source.LinkTo(action);
        return this;
    }

    public IJoinBuilder<TKey, TValue, K2, V2> Join<K2, V2>(string topic)
    {
        return new JoinBuilder<TKey, TValue, K2, V2>(this, _builder, _source, topic);
    }

    public IWithMetadataBuilder WithClientId(string clientId)
    {
        _source.WithClientId(clientId);
        return this;
    }

    public IWithMetadataBuilder WithGroupId(string groupId)
    {
        _source.WithGroupId(groupId);
        return this;
    }
}

public class ConsumeBlock<TKey, TValue> :
    ISourceBlock<Tuple<KafkaContext, TKey, TValue>>
{
    private readonly ISourceBlock<Tuple<KafkaContext, TKey, TValue>> _target;
    private readonly IKafkaConventionBuilder _builder;

    public ConsumeBlock(IKafkaBuilder builder, string topic)
    {
        var buffer = new BufferBlock<Tuple<KafkaContext, TKey, TValue>>();

        _builder = builder.MapTopic(topic, async (KafkaContext context, TKey key, TValue value) =>
        {
            await buffer.SendAsync(Tuple.Create(context, key, value));
            await buffer.Completion;
        });

        _target = buffer;
    }

    public ISourceBlock<Tuple<KafkaContext, TKey, TValue>> WithGroupId(string groupId)
    {
        _builder.WithGroupId(groupId);
        return this;
    }

    public ISourceBlock<Tuple<KafkaContext, TKey, TValue>> WithClientId(string clientId)
    {
        _builder.WithClientId(clientId);
        return this;
    }

    public Task Completion => _target.Completion;

    public void Complete()
    {
        _target.Complete();
    }

    public Tuple<KafkaContext, TKey, TValue>? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TKey, TValue>> target, out bool messageConsumed)
    {
        return _target.ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public void Fault(Exception exception)
    {
        _target.Fault(exception);
    }

    public IDisposable LinkTo(ITargetBlock<Tuple<KafkaContext, TKey, TValue>> target, DataflowLinkOptions linkOptions)
    {
        return _target.LinkTo(target, linkOptions);
    }

    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TKey, TValue>> target)
    {
        _target.ReleaseReservation(messageHeader, target);
    }

    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<Tuple<KafkaContext, TKey, TValue>> target)
    {
        return _target.ReserveMessage(messageHeader, target);
    }
}

public class IntoBlock<TKey, TValue> : 
    ITargetBlock<Tuple<KafkaContext, TKey, TValue>>
{
    private readonly ITargetBlock<Tuple<KafkaContext, TKey, TValue>> _action;

    public IntoBlock(string topic)
    {
        _action = new ActionBlock<Tuple<KafkaContext, TKey, TValue>>(x => 
            Produce(x.Item1, x.Item2, x.Item3, topic));
    }

    public IntoBlock(Func<KafkaContext, TKey, TValue, Task> handler)
    {
        _action = new ActionBlock<Tuple<KafkaContext, TKey, TValue>>(x =>
            handler(x.Item1, x.Item2, x.Item3));
    }

    public Task Completion => _action.Completion;

    private static async Task Produce(KafkaContext context, TKey key, TValue value, string topic)
    {
        await context.ProduceAsync(topic, key, value);
    }

    public void Complete()
    {
        _action.Complete();
    }

    public void Fault(Exception exception)
    {
        _action.Fault(exception);
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, Tuple<KafkaContext, TKey, TValue> messageValue, ISourceBlock<Tuple<KafkaContext, TKey, TValue>>? source, bool consumeToAccept)
    {
        return _action.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }
}