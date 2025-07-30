namespace MinimalKafka.Aggregates.Applier;

internal class ApplierConfigBuilder<TKey, TState, TEvent>(
        IApplierBuilder<TKey, TState, TEvent> builder, 
        EventHandlerConfig<TKey, TState, TEvent> config)
    : IApplierThenBuilder<TKey, TState, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    public IApplierBuilder<TKey, TState, TEvent> Then(Func<TState, TEvent, Result<TState>> handler)
    {
        config.Then = handler;
        return builder;
    }
}

