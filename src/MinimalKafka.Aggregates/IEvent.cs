namespace MinimalKafka.Aggregates;

/// <summary>
/// IEvent interface represents an event in the event sourcing model.
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IEvent<TKey>
    where TKey : notnull
{
    /// <summary>
    /// The unique identifier for the event, typically a GUID.
    /// </summary>
    Guid Id { get; init; }

    /// <summary>
    /// the identifier of the event stream that this event is associated with.
    /// </summary>
    TKey StreamId { get; init; }
}

