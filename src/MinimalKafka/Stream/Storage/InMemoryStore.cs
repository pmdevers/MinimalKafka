using Microsoft.Extensions.Hosting;
using MinimalKafka.Stream.Storage;

namespace MinimalKafka.Stream;

public static class AddKakfkaBuilderExtensions
{
    public static IAddKafkaBuilder WithInMemoryStore(this IAddKafkaBuilder builder)
    {
        return builder.WithStreamStore(typeof(InMemoryStore<,>));
    }
} 


public class InMemoryStore<TKey, TValue>() : BackgroundService, IStreamStore<TKey, TValue>
    where TKey : IEquatable<TKey>
{
    private readonly TimedConcurrentDictionary<TKey, TValue> _dictionary = new(TimeSpan.FromMinutes(3600));
    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        return _dictionary.AddOrUpdate(key, create, update);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            _dictionary.CleanUp();
        }
    }
}

