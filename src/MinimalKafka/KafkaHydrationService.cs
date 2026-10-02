using System.Collections.Concurrent;
using System.Reflection;

namespace MinimalKafka;

internal sealed class KafkaHydrationService(IKafkaFileStore fileStore) : IKafkaHydrationService
{
    private static readonly ConcurrentDictionary<Type, KafkaFilePropertyMetadata[]> _kafkaFilePropertyCache = new();

    private sealed record KafkaFilePropertyMetadata(PropertyInfo Property, bool CanWrite);

    public async Task DeHydrateAsync(object? obj)
    {
        if (obj is null)
        {
            return;
        }

        var props = GetKafkaFileProperties(obj.GetType());

        foreach (var prop in props)
        {
            if (prop.Property.GetValue(obj, null) is not KafkaFile file)
            {
                continue;
            }

            await fileStore.StoreAsync(file.Id, file.Data);
        }
    }

    public async Task ReHydrateAsync(object? obj)
    {
        if (obj is null)
        {
            return;
        }

        var props = GetKafkaFileProperties(obj.GetType());

        foreach (var prop in props)
        {
            if (prop.Property.GetValue(obj, null) is not KafkaFile file)
            {
                continue;
            }

            var data = await fileStore.LoadAsync(file.Id);

            if (prop.CanWrite)
            {
                prop.Property.SetValue(obj, file with { Data = data });
            }
        }
    }

    private static KafkaFilePropertyMetadata[] GetKafkaFileProperties(Type type)
        => _kafkaFilePropertyCache.GetOrAdd(type, static t =>
            [.. t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.PropertyType == typeof(KafkaFile) && x.CanRead)
                .Select(x => new KafkaFilePropertyMetadata(x, x.CanWrite))]);
}
