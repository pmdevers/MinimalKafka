using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serialization;
using System.Text;

namespace MinimalKafka.Stream;

/// <summary>
/// Helpers for working with strongly typed stream stores.
/// </summary>
public static class KafkaStoreExtensions
{
    /// <summary>Reads and deserializes a stored value for the specified key.</summary>
    public static async ValueTask<TValue?> FindByKeyAsync<TKey, TValue>(
        this IKafkaStore store,
        TKey key,
        CancellationToken cancellationToken = default,
        string? format = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(key);

        var keyBytes = await StreamSerialization.SerializeAsync(
            store.ServiceProvider,
            store.TopicName,
            key,
            [],
            cancellationToken,
            format).ConfigureAwait(false);

        var valueBytes = await store.FindByKeyAsync(keyBytes).ConfigureAwait(false);
        return valueBytes is null
            ? default
            : await StreamSerialization.DeserializeAsync<TValue>(
                store.ServiceProvider,
                valueBytes,
                store.TopicName,
                [],
                cancellationToken,
                format).ConfigureAwait(false);
    }

    /// <summary>Stores a strongly typed value using the specified key.</summary>
    public static async ValueTask<TValue> AddOrUpdateAsync<TKey, TValue>(
        this IKafkaStore store,
        TKey key,
        TValue value,
        CancellationToken cancellationToken = default,
        string? format = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var keyBytes = await StreamSerialization.SerializeAsync(
            store.ServiceProvider,
            store.TopicName,
            key,
            [],
            cancellationToken,
            format).ConfigureAwait(false);

        var valueBytes = await StreamSerialization.SerializeAsync(
            store.ServiceProvider,
            store.TopicName,
            value,
            [],
            cancellationToken,
            format).ConfigureAwait(false);

        await store.AddOrUpdate(keyBytes, valueBytes).ConfigureAwait(false);
        return value;
    }
}
