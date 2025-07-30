namespace MinimalKafka.Aggregates.Storage;

/// <summary>
/// The IAggregateStore interface defines the contract for a storage mechanism
/// </summary>
/// <typeparam name="TKey">The key of the stream.</typeparam>
/// <typeparam name="TState">The Aggregate to store.</typeparam>
public interface IAggregateStore<TKey, TState>
    where TKey : notnull
    where TState : IAggregate<TKey>
{
    /// <summary>
    /// Finds an aggregate by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the aggregate.</param>
    /// <returns></returns>
    Task<TState?> FindByIdAsync(TKey id);

    /// <summary>
    /// Saves an aggregate.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    Task SaveAsync(TState state);
        
}

