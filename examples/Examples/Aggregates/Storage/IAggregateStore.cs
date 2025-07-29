namespace Examples.Aggregates.Storage;

public interface IAggregateStore<TKey, TState>
    where TKey : notnull
    where TState : IAggregate<TKey>
{
    Task<TState?> FindByIdAsync(TKey k);
    Task SaveAsync(TState state);
        
}

