using Microsoft.Extensions.Hosting;

namespace MinimalKafka.Stream.Storage;

internal sealed class InMemoryStore<TKey, TValue>() : BackgroundService, IStreamStore<TKey, TValue>
    where TKey : IEquatable<TKey>
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

