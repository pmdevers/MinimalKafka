using System.Collections.Concurrent;

namespace MinimalKafka.Stream;

public class InMemoryStore<TKey, TValue> : IStreamStore<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = [];
    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        return _dictionary.AddOrUpdate(key, create, update);
    }
}

