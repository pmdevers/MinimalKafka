using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using System.Text;
using System.Text.Json;

namespace MinimalKafka.Stream;
public interface IAggregateBuilder<TKey, TState>
    where TState : Aggregate<TKey>, IAggregate<TKey, TState>
{
    IAggregateBuilder<TKey, TState> AddEvent<TEvent>(Func<TState, TEvent, TState> handler);
}
public class AggregateBuilder<TKey, TState>(IIntoBuilder<TKey, (AggregateEvent<TKey>?, TState?)> builder, string topic)
    : IAggregateBuilder<TKey, TState>
    where TState : Aggregate<TKey>, IAggregate<TKey, TState>
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    private readonly Dictionary<string, Func<TKey, TState, AggregateEvent<TKey>, Task<TState>>> _handlers = [];
    private readonly IIntoBuilder<TKey, (AggregateEvent<TKey>?, TState?)> _builder = builder;

    public IAggregateBuilder<TKey, TState> AddEvent<TEvent>(Func<TState, TEvent, TState> handler)
    {
        _handlers.Add(typeof(TEvent).Name, async (k, v, e) =>
        {
            var payload = await GetPayload<TEvent>(e);
            var aggregate = handler(v, payload);
            return aggregate;
        });
        return this;
    }

    public IKafkaConventionBuilder Build()
    {
        return _builder.Into(async (context, key, value) =>
        {
            Console.WriteLine($"Event Received: {value.Item1?.Name}");

            if (value.Item1 == null)
                return;

            var state = value.Item2 ?? TState.Create(context, key);

            var result = await _handlers[value.Item1.Name](key, state, value.Item1);

            if (result == state)
            {
                return;
            }

            await context.ProduceAsync(topic, key, result);
        });
    }

    

    private async Task<T> GetPayload<T>(AggregateEvent<TKey> @event)
    {
        var jsonString = JsonSerializer.Serialize(@event.Payload);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
        var result = await JsonSerializer.DeserializeAsync<T>(stream, _options);
        return result is null ? throw new InvalidCastException("Payload can not be read") : result;
    }
}

public static class AggregateBuilderExtentsions
{
    public static IKafkaConventionBuilder Aggregate<TKey, TValue>(this IStreamBuilder<TKey, AggregateEvent<TKey>> builder, string topic,
        Action<IAggregateBuilder<TKey, TValue>> config)
        where TValue : Aggregate<TKey>, IAggregate<TKey, TValue>
    {
        var intoBuilder = builder.Join<TKey, TValue>(topic).OnKey();
        var build = new AggregateBuilder<TKey, TValue>(intoBuilder, topic);
        config(build);
        return build.Build();
    }
}

public record AggregateEvent<TKey>(TKey Id, string Name, object Payload);
public record Aggregate<TKey>(TKey Id, string Name) 
    : IAggregate<TKey, Aggregate<TKey>>
{
    public static Aggregate<TKey> Create(KafkaContext context, TKey key)
        => new(key, string.Empty);

    public string SurName { get; set; } = string.Empty;
}

public interface IAggregate<in TKey, out TSelf>
{
    abstract static TSelf Create(KafkaContext context, TKey key);
}