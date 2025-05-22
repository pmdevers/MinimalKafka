using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Blocks;

public class IntoBlock<TValue>(Func<KafkaContext, TValue, Task> handler) 
    : ITargetBlock<(KafkaContext, TValue)>
{
    private readonly ITargetBlock<(KafkaContext, TValue)> _block = 
        new ActionBlock<(KafkaContext, TValue)>(async (data) => {
            try
            {
                await handler(data.Item1, data.Item2);
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        });

    public Task Completion => _block.Completion;

    public void Complete()
    {
        _block.Complete();
    }

    public void Fault(Exception exception)
    {
        _block.Fault(exception);
    }

    public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, (KafkaContext, TValue) messageValue, ISourceBlock<(KafkaContext, TValue)>? source, bool consumeToAccept)
    {
        return _block.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
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