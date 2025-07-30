namespace MinimalKafka.Aggregates.Applier;

/// <summary>
/// Captures the configuration for an event handler in an aggregate applier.
/// </summary>
/// <typeparam name="TKey">The key of the eventstream</typeparam>
/// <typeparam name="TState">The state to apply the event to.</typeparam>
/// <typeparam name="TEvent">The event that needs to be applied.</typeparam>
/// <param name="when">The condition this handler is triggerd.</param>
public class EventHandlerConfig<TKey, TState, TEvent>(Func<TEvent, bool> when)
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// the condition that triggers this handler.
    /// </summary>
    public Func<TEvent, bool> When { get; set; } = when;

    /// <summary>
    /// The function that applies the event to the state.
    /// </summary>
    public Func<TState, TEvent, Result<TState>> Then { get;set; } = (s, _) => s;
};

