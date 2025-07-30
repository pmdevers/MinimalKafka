namespace MinimalKafka.Aggregates.Applier;


/// <summary>
/// A builder interface for configuring event handlers in an aggregate applier.
/// </summary>
/// <typeparam name="TKey">The unique identifier of the event stream</typeparam>
/// <typeparam name="TState">The state to applie the events to.</typeparam>
/// <typeparam name="TEvent">The event that needs to be applied.</typeparam>
public interface IApplierBuilder<TKey, TState, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// Adds an event handler configuration to the applier builder.
    /// </summary>
    /// <param name="handler"></param>
    void Add(EventHandlerConfig<TKey, TState, TEvent> handler);
}

