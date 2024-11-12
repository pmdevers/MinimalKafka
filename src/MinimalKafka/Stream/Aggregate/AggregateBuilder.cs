using MinimalKafka.Builders;
using System.Text;
using System.Text.Json;

namespace MinimalKafka.Stream;
public interface IAggregateBuilder<TKey, TState>
    where TState : Aggregate<TKey>, IAggregate<TKey, TState>
{
    IAggregateBuilder<TKey, TState> AddEvent(string name, Func<TKey, TState, AggregateEvent<TKey>, Task<TState>> handler);
    IAggregateBuilder<TKey, TState> AddEvent<TEvent>(Func<TState, TEvent, TState> handler);
}
public class AggregateBuilder<TKey, TState>(IIntoBuilder<TKey, (AggregateEvent<TKey>?, TState?)> builder, string topic)
    : IAggregateBuilder<TKey, TState>
    where TState : Aggregate<TKey>, IAggregate<TKey, TState>
{
    private readonly Dictionary<string, Func<TKey, TState, AggregateEvent<TKey>, Task<TState>>> _handlers = [];
    private readonly IIntoBuilder<TKey, (AggregateEvent<TKey>?, TState?)> _builder = builder;

    public IAggregateBuilder<TKey, TState> AddEvent(string name, Func<TKey, TState, AggregateEvent<TKey>, Task<TState>> handler)
    {
        _handlers.Add(name, handler);
        return this;
    }

    public IAggregateBuilder<TKey, TState> AddEvent<TEvent>(
       Func<TState, TEvent, TState> handler)
    {
        JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        return AddEvent(typeof(TEvent).Name, async (k, v, e) =>
        {
            var jsonString = JsonSerializer.Serialize(e.Payload);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            var result = await JsonSerializer.DeserializeAsync<TEvent>(stream, _options);
            var payload = result is null ? throw new InvalidCastException("Payload can not be read") : result;
            var aggregate = handler(v, payload);
            return aggregate;
        });
    }

    public IKafkaConventionBuilder Build()
    {
        return _builder.Into(async (context, key, value) =>
        {
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
}
