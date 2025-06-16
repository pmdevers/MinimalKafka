namespace MinimalKafka.Stream;

/// <summary>
/// Defines a contract for a stream store that supports adding, updating, and querying values by key or predicate.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify values in the store.</typeparam>
/// <typeparam name="TValue">The type of the values stored.</typeparam>
public interface IStreamStore<TKey, TValue>
{
    /// <summary>
    /// Adds a new value for the specified key or updates the existing value.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="create">A function to create a new value if the key does not exist.</param>
    /// <param name="update">A function to update the existing value if the key exists.</param>
    /// <returns>A <see cref="ValueTask{TValue}"/> representing the asynchronous operation, with the added or updated value.</returns>
    ValueTask<TValue> AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update);

    /// <summary>
    /// Finds a value by its key asynchronously.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>
    /// A <see cref="ValueTask{TValue}"/> representing the asynchronous operation, with the found value if it exists; otherwise, <c>null</c>.
    /// </returns>
    ValueTask<TValue?> FindByIdAsync(TKey key);

    /// <summary>
    /// Finds all values that match the specified predicate.
    /// </summary>
    /// <param name="predicate">A function to test each value for a condition.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> that yields all values matching the predicate.
    /// </returns>
    IAsyncEnumerable<TValue> FindAsync(Func<TValue, bool> predicate);
}

