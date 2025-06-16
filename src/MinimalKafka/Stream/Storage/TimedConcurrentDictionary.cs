using System.Collections.Concurrent;

namespace MinimalKafka.Stream.Storage;

/// <summary>
/// A thread-safe dictionary that stores key-value pairs with an associated expiration time.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
/// <remarks>
/// This dictionary automatically removes entries that have exceeded the specified expiration time.
/// </remarks>
/// <param name="expirationTime">The time duration after which items in the dictionary expire and are removed.</param>
internal sealed class TimedConcurrentDictionary<TKey, TValue>(TimeSpan expirationTime) : IStreamStore<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, Tuple<TValue, DateTimeOffset>> _dictionary = [];
    private readonly TimeSpan _expirationTime = expirationTime;

    /// <summary>
    /// Adds a new value to the dictionary or updates an existing value if the key is already present.
    /// </summary>
    /// <param name="key">The key associated with the value.</param>
    /// <param name="create">A function to create a new value if the key is not present in the dictionary.</param>
    /// <param name="update">A function to update the existing value if the key is present in the dictionary.</param>
    /// <returns>The newly added or updated value.</returns>
    public ValueTask<TValue> AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        var result = _dictionary.AddOrUpdate(key,
            (k) => new Tuple<TValue, DateTimeOffset>(create(k), TimeProvider.System.GetUtcNow()),
            (k, v) => Tuple.Create(update(k, v.Item1), TimeProvider.System.GetUtcNow()));

        return ValueTask.FromResult(result.Item1);
    }

    /// <summary>
    /// Removes expired items from the dictionary based on the expiration time.
    /// </summary>
    public void CleanUp()
    {
        var now = TimeProvider.System.GetUtcNow();

        // Remove items where the timestamp is older than the expiration time
        foreach (var key in _dictionary.Keys.ToList())
        {
            if (now - _dictionary[key].Item2 > _expirationTime)
            {
                _dictionary.TryRemove(key, out _);
            }
        }
    }

    public IAsyncEnumerable<TValue> FindAsync(Func<TValue, bool> predicate)
    {
        return _dictionary
            .Where(x => predicate(x.Value.Item1))
            .Select(x => x.Value.Item1).ToAsyncEnumerable();
    }

    public ValueTask<TValue?> FindByIdAsync(TKey key)
    {
        if(!_dictionary.TryGetValue(key, out var result))
        {
            return ValueTask.FromResult(default(TValue));
        }

        return ValueTask.FromResult<TValue?>(result.Item1);
    }

    /// <summary>
    /// Retrieves all non-expired items from the dictionary as a new dictionary.
    /// </summary>
    /// <returns>A <see cref="ConcurrentDictionary{TKey, TValue}"/> containing the current non-expired key-value pairs.</returns>
    public ConcurrentDictionary<TKey, TValue> GetItems()
    {
        var result = new ConcurrentDictionary<TKey, TValue>();

        foreach (var kvp in _dictionary)
        {
            result[kvp.Key] = kvp.Value.Item1;
        }

        return result;
    }
}

