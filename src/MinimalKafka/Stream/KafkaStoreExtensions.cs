using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serializers;

namespace MinimalKafka.Stream;


/// <summary>
/// Extension methods for querying data from an <see cref="IKafkaStore"/> using serializers.
/// </summary>
public static class KafkaStoreExtensions
{

    /// <summary>
    /// Finds and deserializes a value from the store using the provided key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key to search for.</typeparam>
    /// <typeparam name="TValue">The type of the value to deserialize.</typeparam>
    /// <param name="store">The Kafka store instance.</param>
    /// <param name="key">The key to look up in the store.</param>
    /// <returns>
    /// A <typeparamref name="TValue"/> instance if found; otherwise <c>null</c>.
    /// </returns>
    public async static ValueTask<TValue?> FindByKey<TKey, TValue>(this IKafkaStore store, TKey key)
    {
        var keySerializer = store.ServiceProvider.GetRequiredService<IKafkaSerializer<TKey>>();
        var valueSerializer = store.ServiceProvider.GetRequiredService<IKafkaSerializer<TValue>>();

        var keyVal = keySerializer.Serialize(key);
        var value = await store.FindByIdAsync(keyVal) ?? [];

        return valueSerializer.Deserialize(value);
    }

    /// <summary>
    /// Asynchronously enumerates all deserialized values from the store that match the given predicate.
    /// </summary>
    /// <typeparam name="TValue">The type of the values to retrieve and filter.</typeparam>
    /// <param name="store">The Kafka store instance.</param>
    /// <param name="predicate">
    /// A function to test each value for a condition.
    /// </param>
    /// <returns>
    /// An asynchronous stream of <typeparamref name="TValue"/> instances matching the predicate.
    /// </returns>
    public static async IAsyncEnumerable<TValue> FindAsync<TValue>(this IKafkaStore store, Func<TValue, bool> predicate)
    {
        var valueSerializer = store.ServiceProvider.GetRequiredService<IKafkaSerializer<TValue>>();

        await foreach (var item in store.GetItems())
        {
            var val = valueSerializer.Deserialize(item);

            if (val is not null && predicate(val))
            {
                yield return val;
            }
        }
    }
}
