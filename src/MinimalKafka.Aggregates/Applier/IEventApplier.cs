namespace MinimalKafka.Aggregates.Applier;

/// <summary>
/// Interface for defining an event applier that applies events to an aggregate state.
/// </summary>
/// <typeparam name="TKey">The unique idenifier that of the event stream.</typeparam>
/// <typeparam name="TState">The state to apply the event to.</typeparam>
/// <typeparam name="TEvent">The event that needs to be applied.</typeparam>
public interface IEventApplier<TKey, TState, TEvent> 
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// Configures the event applier with the specified builder.
    /// </summary>
    /// <param name="builder"></param>
    abstract static void Configure(IApplierBuilder<TKey, TState, TEvent> builder);
}
