using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

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

    public IServiceProvider ServiceProvider => serviceProvider;

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

    public void CleanUp()
    {
        _store.CleanUp();
    }

    public async ValueTask<byte[]> FindByIdAsync(byte[] key)
    {
        return await _store.FindByIdAsync(key) ?? [];
    }

    public IAsyncEnumerable<byte[]> GetItems()
    {
        return _store.GetItems().Values.ToAsyncEnumerable();
    }
}