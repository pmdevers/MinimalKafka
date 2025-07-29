namespace Examples.Aggregates.Applier;

public interface IEventApplier<TKey, TState, TEvent> 
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    abstract static void Configure(IApplierBuilder<TKey, TState, TEvent> builder);
}
