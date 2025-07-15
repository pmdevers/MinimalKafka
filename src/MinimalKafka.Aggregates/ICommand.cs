namespace MinimalKafka.Aggregates;

/// <summary>
/// Represents a command for an aggregate in a domain-driven design context,
/// providing identification, versioning, and command naming metadata.
/// </summary>
/// <typeparam name="TKey">
/// The type used for the aggregate's unique identifier.
/// </typeparam>
public interface ICommand<out TKey>
{
    /// <summary>
    /// Gets the unique identifier of the aggregate for which this command is intended.
    /// </summary>
    TKey Id { get; }

    /// <summary>
    /// Gets the expected version of the aggregate at the time the command should be applied.
    /// Used for optimistic concurrency checks.
    /// </summary>
    int Version { get; }
}
