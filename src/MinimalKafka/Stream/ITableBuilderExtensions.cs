using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serializers;

namespace MinimalKafka.Stream;

/// <summary>
/// Provides extension methods for converting stream builders into table builders.
/// </summary>
public static class ITableBuilderExtensions
{
    /// <summary>
    /// Converts a stream builder into a table writer that persists key-value pairs to a Kafka-backed table.
    /// </summary>
    /// <typeparam name="TKey">The type of the message key.</typeparam>
    /// <typeparam name="TValue">The type of the message value.</typeparam>
    /// <param name="builder">The stream builder to convert.</param>
    /// <param name="tableName">The name of the table to write to.</param>
    /// <returns>A Kafka convention builder for further configuration.</returns>
    public static IKafkaConventionBuilder ToTable<TKey, TValue>(this IIntoBuilder<TKey, TValue> builder, string tableName)
    {
        return builder.Into(async (c, k, v) => {
            var factory = c.RequestServices.GetRequiredService<IKafkaStoreFactory>();
            var keySerializer = c.RequestServices.GetRequiredService<IKafkaSerializer<TKey>>();
            var valueSerializer = c.RequestServices.GetRequiredService<IKafkaSerializer<TValue>>();
            var table = factory.GetStore(tableName);
            var keyBytes = keySerializer.Serialize(k);
            var valueBytes = valueSerializer.Serialize(v);
            await table.AddOrUpdate(keyBytes, valueBytes);
        });
    }
}
