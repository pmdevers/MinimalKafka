namespace MinimalKafka.Aggregates.Applier;

/// <summary>
/// A builder interface for configuring the next step in an event handler chain in an aggregate applier.
/// </summary>
/// <typeparam name="TKey">The unique identifier of the event stream.</typeparam>
/// <typeparam name="TState">The state to apply the event to.</typeparam>
/// <typeparam name="TEvent">The event that needs to be applied.</typeparam>
public interface IApplierThenBuilder<TKey, TState, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// The handler that applies the event to the state.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <returns></returns>
    IApplierBuilder<TKey, TState, TEvent> Then(Func<TState, TEvent, Result<TState>> handler);
}

