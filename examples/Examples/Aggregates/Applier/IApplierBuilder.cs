namespace Examples.Aggregates.Applier;

public interface IApplierBuilder<TKey, TState, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    void Add(EventHandlerConfig<TKey, TState, TEvent> handler);
}

