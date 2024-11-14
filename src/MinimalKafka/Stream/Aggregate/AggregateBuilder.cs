using MinimalKafka.Builders;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MinimalKafka.Stream;
public interface IAggregateBuilder<TKey, TAggregate>
    where TAggregate : Aggregate<TKey>, IAggregate<TKey, TAggregate>
{

    IAggregateBuilder<TKey, TAggregate> AddCommand<TCommand>() where TCommand : ICommand<TAggregate>;
}
public class AggregateBuilder<TKey, TAggregate>(IIntoBuilder<TKey, (AggregateCommand<TKey>?, TAggregate?)> builder, string topic)
    : IAggregateBuilder<TKey, TAggregate>
    where TAggregate : Aggregate<TKey>, IAggregate<TKey, TAggregate>
{
    private readonly Dictionary<string, Func<AggregateCommand<TKey>, ValueTask<ICommand<TAggregate>?>>> _handlers = [];
    private readonly IIntoBuilder<TKey, (AggregateCommand<TKey>?, TAggregate?)> _builder = builder;


    public IAggregateBuilder<TKey, TAggregate> AddCommand<TCommand>()
        where TCommand : ICommand<TAggregate>
    {
        _handlers.Add(typeof(TCommand).Name, async (e) => await GetCommand<TCommand>(e));
        return this;
    }

    public IKafkaConventionBuilder Build()
    {
        return _builder.Into(async (context, key, value) =>
        {
            if (value.Item1 == null)
                return;

            if (!_handlers.TryGetValue(value.Item1.Name, out var command))
            {
                throw new InvalidOperationException("Command not registred");
            }

            var c = await command.Invoke(value.Item1);
            var result = c?.Execute(value.Item2 ?? TAggregate.Create(context, key));
            
            if (result == value.Item2)
            {
                return;
            }

            await context.ProduceAsync(topic, key, result);
        });
    }

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private ValueTask<TCommand?> GetCommand<TCommand>(AggregateCommand<TKey> e)
        where TCommand : ICommand<TAggregate>
    {
        var jsonString = JsonSerializer.Serialize(e.Payload);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
        return JsonSerializer.DeserializeAsync<TCommand>(stream, _options);
    }
}
