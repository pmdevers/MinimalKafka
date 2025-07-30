using System.Collections.Concurrent;

namespace MinimalKafka.Aggregates.Storage;

internal class InMemoryStateStore<TKey, TState> : IAggregateStore<TKey, TState>
    where TKey : notnull
    where TState : IAggregate<TKey>
{
    private readonly ConcurrentDictionary<TKey, TState> _values = [];

    public Task<TState?> FindByIdAsync(TKey id)
    {
        _values.TryGetValue(id, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(TState state)
    {
        _values.AddOrUpdate(state.Id, (_) => state, (_, _) => state);
        return Task.CompletedTask;
    }
}

