using Microsoft.Extensions.Caching.Memory;
using MinimalKafka.Stream;

namespace KafkaAdventure;

public class StreamStore<TKey, TValue>(IMemoryCache cache) : IStreamStore<TKey, TValue>
{
    public ValueTask<TValue> AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        if(cache.TryGetValue(key!, out TValue? value) ){
            return new ValueTask<TValue>(cache.Set(key!, update(key, value!)));
        } else
        {
            return new ValueTask<TValue>(cache.Set(key!, create(key)));
        };
    }

    public IAsyncEnumerable<TValue> FindAsync(Func<TValue, bool> predicate)
        => AsyncEnumerable.Empty<TValue>();

    public ValueTask<TValue?> FindByIdAsync(TKey key)
    {
        return new ValueTask<TValue?>(cache.Get<TValue>(key!));
    }
}
