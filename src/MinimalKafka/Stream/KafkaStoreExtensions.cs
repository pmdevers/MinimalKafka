using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serializers;

namespace MinimalKafka.Stream;
internal static class KafkaStoreExtensions
{
    public async static ValueTask<TValue?> FindByKey<TKey, TValue>(this IKafkaStore store, TKey key)
    {
        var keySerializer = store.ServiceProvider.GetRequiredService<IKafkaSerializer<TKey>>();
        var valueSerializer = store.ServiceProvider.GetRequiredService<IKafkaSerializer<TValue>>();

        var keyVal = keySerializer.Serialize(key);
        var value = await store.FindByIdAsync(keyVal) ?? [];

        return valueSerializer.Deserialize(value);
    }

    public async static IAsyncEnumerable<TValue> FindAsync<TValue>(this IKafkaStore store, Func<TValue, bool> value)
    {
        var valueSerializer = store.ServiceProvider.GetRequiredService<IKafkaSerializer<TValue>>();

        await foreach (var item in store.GetItems())
        {
            var val = valueSerializer.Deserialize(item);

            if (val is not null && value(val))
            {
                yield return val;
            }
        }
    }
}
