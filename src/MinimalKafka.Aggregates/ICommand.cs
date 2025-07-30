namespace MinimalKafka.Aggregates;

/// <summary>
/// This interface represents a command that can be sent to an aggregate.
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface ICommand<out TKey>
    where TKey : notnull
{
    /// <summary>
    /// The unique identifier for the command, typically a GUID.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The identifier of the event stream that this command is intended for.
    /// </summary>
    TKey StreamId { get; }
}
