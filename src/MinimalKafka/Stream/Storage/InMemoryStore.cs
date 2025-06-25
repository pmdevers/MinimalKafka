using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace MinimalKafka.Stream.Storage;

internal sealed class InMemoryStreamStoreFactory : IStreamStoreFactory
{
    private readonly ConcurrentBag<object> _stores = [];

    public IStreamStore<TKey, TValue> GetStreamStore<TKey, TValue>()
        where TKey : notnull
    {
        var item = _stores.OfType<InMemoryStore<TKey, TValue>>().FirstOrDefault();
        if(item == null)
        {
            item = new InMemoryStore<TKey, TValue>();
            _stores.Add(item);
        }
        return item;
    }
}

internal sealed class InMemoryStore<TKey, TValue>() : BackgroundService, IStreamStore<TKey, TValue>
    where TKey : notnull
{
    private readonly TimedConcurrentDictionary<TKey, TValue> _dictionary = new(TimeSpan.FromMinutes(3600));

    public ValueTask<TValue> AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        return _dictionary.AddOrUpdate(key, create, update);
    }

    public IAsyncEnumerable<TValue> FindAsync(Func<TValue, bool> predicate)
    {
        return _dictionary.FindAsync(predicate);
    }

    public ValueTask<TValue?> FindByIdAsync(TKey key)
    {
        return _dictionary.FindByIdAsync(key);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            _dictionary.CleanUp();
        }
    }
}

