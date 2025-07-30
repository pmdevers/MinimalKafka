namespace MinimalKafka.Aggregates;

/// <summary>
/// interface representing an aggregate root in the domain model.
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IAggregate<TKey>
    where TKey : notnull
{
    /// <summary>
    /// The unique identifier for the aggregate.
    /// </summary>
    TKey Id { get; init; }

    /// <summary>
    /// The version of the aggregate, used for concurrency control.
    /// </summary>
    int Version { get; init; }
}

