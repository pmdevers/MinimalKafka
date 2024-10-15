using Microsoft.Extensions.Hosting;
using MinimalKafka.Stream.Storage;
using System.Collections.Concurrent;

namespace MinimalKafka.Stream;

public class InMemoryStore<TKey, TValue>() : BackgroundService, IStreamStore<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new();
    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        return _dictionary.AddOrUpdate(key, create, update);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {

            await Task.Delay(3000, stoppingToken);
            Console.WriteLine("Clean up.");
        }
    }
}

