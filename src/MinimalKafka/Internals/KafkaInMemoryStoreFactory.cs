using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalKafka.Serializers;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MinimalKafka.Internals;

internal class KafkaInMemoryStoreFactory(IServiceProvider serviceProvider) : BackgroundService, IKafkaStoreFactory
{
    private readonly ConcurrentDictionary<KafkaConsumerKey, KafkaInMemoryStore> _stores = [];
    private readonly object _lock = new();

    public IKafkaStore GetStore(KafkaConsumerKey consumerKey)
    {
        lock (_lock)
        {
            if (!_stores.TryGetValue(consumerKey, out KafkaInMemoryStore? store))
            {
                store = new KafkaInMemoryStore(serviceProvider);
                _stores.TryAdd(consumerKey, store);

            }
            return store;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            foreach(var store in _stores.Values) 
            { 
                store.CleanUp();
            }
        }
    }
}


internal class KafkaInMemoryStore(IServiceProvider serviceProvider) : IKafkaStore
{
    private readonly TimedConcurrentDictionary<byte[], byte[]> _store =
        new(TimeSpan.FromDays(7));

    public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        byte[] localKey = new byte[key.Length];
        byte[] localVal = new byte[value.Length];
        key.CopyTo(localKey);
        value.CopyTo(localVal);

        return _store.AddOrUpdate(localKey, 
            (k) => localVal, 
            (k, v) => localVal);
    }

    public async ValueTask<TValue?> FindByKey<TKey, TValue>(TKey key)
    {
        var keySerializer = serviceProvider.GetRequiredService<IKafkaSerializer<TKey>>();
        var valueSerializer = serviceProvider.GetRequiredService<IKafkaSerializer<TValue>>();

        var keyVal = keySerializer.Serialize(key);
        var value = await _store.FindByIdAsync(keyVal) ?? [];

        return valueSerializer.Deserialize(value);
    }

    public void CleanUp()
    {
        _store.CleanUp();
    }

    public async IAsyncEnumerable<TValue> FindAsync<TValue>(Func<TValue, bool> value)
    {
        var valueSerializer = serviceProvider.GetRequiredService<IKafkaSerializer<TValue>>();

        await foreach(var item in _store.GetItems().ToAsyncEnumerable())
        {
            var val = valueSerializer.Deserialize(item.Value);

            if (val is not null && value(val))
            {
                yield return val;
            }
        }
    }
}