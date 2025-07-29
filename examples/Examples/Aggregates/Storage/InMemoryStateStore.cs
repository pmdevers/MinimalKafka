using System.Collections.Concurrent;

namespace Examples.Aggregates.Storage;

public class InMemoryStateStore<TKey, TState> : IAggregateStore<TKey, TState>
    where TKey : notnull
    where TState : IAggregate<TKey>
{
    private readonly ConcurrentDictionary<TKey, TState> _values = [];

    public Task<TState?> FindByIdAsync(TKey k)
    {
        _values.TryGetValue(k, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(TState state)
    {
        _values.AddOrUpdate(state.Key, (_) => state, (_, _) => state);
        return Task.CompletedTask;
    }
}

