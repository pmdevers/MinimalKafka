using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serializers;

namespace MinimalKafka.Stream;

/// <summary>
/// 
/// </summary>
public static class ITableBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="builder"></param>
    /// <param name="tableName"></param>
    /// <returns></returns>
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
