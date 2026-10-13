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
    extension(IKafkaStore store)
    {
        /// <summary>Reads and deserializes a stored value for the specified key.</summary>
        public async ValueTask<TValue?> FindByKeyAsync<TKey, TValue>(
            TKey key,
            string? format = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(key);

            var keyBytes = await StreamSerialization.SerializeAsync(
                store.ServiceProvider,
                store.TopicName,
                key,
                [],
                format,
                cancellationToken).ConfigureAwait(false);

            var valueBytes = await store.FindByKeyAsync(keyBytes).ConfigureAwait(false);
            return valueBytes is null
                ? default
                : await StreamSerialization.DeserializeAsync<TValue>(
                    store.ServiceProvider,
                    valueBytes,
                    store.TopicName,
                    [],
                    format,
                    cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Stores a strongly typed value using the specified key.</summary>
        public async ValueTask<TValue> AddOrUpdateAsync<TKey, TValue>(
            TKey key,
            TValue value,
            string? format = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            var keyBytes = await StreamSerialization.SerializeAsync(
                store.ServiceProvider,
                store.TopicName,
                key,
                [],
                format,
                cancellationToken).ConfigureAwait(false);

            var valueBytes = await StreamSerialization.SerializeAsync(
                store.ServiceProvider,
                store.TopicName,
                value,
                [],
                format,
                cancellationToken).ConfigureAwait(false);

            await store.AddOrUpdate(keyBytes, valueBytes).ConfigureAwait(false);
            return value;
        }
    }
}
