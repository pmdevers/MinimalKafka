namespace Examples.Aggregates.Applier;

public interface IApplierThenBuilder<TKey, TState, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    IApplierBuilder<TKey, TState, TEvent> Then(Func<TState, TEvent, Result<TState>> handler);
}

