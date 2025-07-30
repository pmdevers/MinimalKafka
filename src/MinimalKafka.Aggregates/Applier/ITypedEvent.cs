using MinimalKafka.Aggregates;

namespace MinimalKafka.Aggregates.Applier;

/// <summary>
/// an interface for events that have a specific type.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TEventTypes"></typeparam>
public interface ITypedEvent<TKey, TEventTypes> : IEvent<TKey>
    where TKey : notnull
    where TEventTypes : struct, Enum
{
    /// <summary>
    /// the type of the event, typically an enum value that represents the event type.
    /// </summary>
    TEventTypes Type { get; init; }
}

