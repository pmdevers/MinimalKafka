using Examples.Aggregates.Storage;
using MinimalKafka;

namespace Examples.Aggregates.Applier;

internal class ApplierBuilder<TKey, TState, TEvent>() 
    : IApplierBuilder<TKey, TState, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>, new()
    where TEvent: IEvent<TKey>
{
    private readonly ICollection<EventHandlerConfig<TKey, TState, TEvent>> _branches = [];

    public void Add(EventHandlerConfig<TKey, TState, TEvent> handler)
    {
        _branches.Add(handler);
    }

    public Func<KafkaContext, TKey, TEvent, Task> Build()
    {
        return async (c, k, @event) =>
        {
            if(@event is null)
            { 
                return; 
            }

            var branch = _branches.FirstOrDefault(x => x.When(@event));

            if(branch is null)
                return;

            var store = c.RequestServices.GetRequiredService<IAggregateStore<TKey, TState>>();
            var state = await store.FindByIdAsync(k);

            state ??= new TState() { Key = k };

            var result = branch.Then(state, @event);

            if (result.IsSuccess)
            {
                await store.SaveAsync(result.State);
            }
        };
    }
}
