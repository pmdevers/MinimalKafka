using Microsoft.Extensions.Caching.Memory;
using MinimalKafka.Stream;

namespace KafkaAdventure;

public class StreamStore<TKey, TValue>(IMemoryCache cache) : IStreamStore<TKey, TValue>
{
    /// <summary>
    /// Adds a new value to the cache for the specified key or updates the existing value using the provided functions.
    /// </summary>
    /// <param name="key">The key associated with the value to add or update.</param>
    /// <param name="create">A function to create a new value if the key does not exist.</param>
    /// <param name="update">A function to update the existing value if the key is found.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> containing the added or updated value.</returns>
    public ValueTask<TValue> AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        if(cache.TryGetValue(key!, out TValue? value) ){
            return new ValueTask<TValue>(cache.Set(key!, update(key, value!)));
        } else
        {
            return new ValueTask<TValue>(cache.Set(key!, create(key)));
        };
    }

    /// <summary>
        /// Returns an empty asynchronous sequence of values, ignoring the provided predicate.
        /// </summary>
        public IAsyncEnumerable<TValue> FindAsync(Func<TValue, bool> predicate)
        => AsyncEnumerable.Empty<TValue>();

    /// <summary>
    /// Retrieves the value associated with the specified key from the cache asynchronously.
    /// </summary>
    /// <param name="key">The key whose value should be retrieved.</param>
    /// <returns>A ValueTask containing the value if found; otherwise, null.</returns>
    public ValueTask<TValue?> FindByIdAsync(TKey key)
    {
        return new ValueTask<TValue?>(cache.Get<TValue>(key!));
    }
}
