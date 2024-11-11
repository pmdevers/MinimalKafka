using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;
using MinimalKafka.Stream;
using System.Threading.Tasks.Dataflow;
using MinimalKafka;
using System.Text.Json;
using System.Text;
using MinimalKafka.Extension;

namespace Examples;

public static class Process
{
    public static void Map(WebApplication app)
    {
        var kb = app.Services.GetRequiredService<IKafkaBuilder>();
        var leftStore = new InMemoryStore<Guid, LeftObject>();
        var rightStore = new InMemoryStore<int, RightObject>();

        var left = new ConsumeBlock<Guid, LeftObject>(kb, "left");
        var right = new ConsumeBlock<int, RightObject>(kb, "right");

        var join = new JoinBlock<Guid, LeftObject, int, RightObject>(leftStore, rightStore, (l, r) => l.RightObjectId == r.Id);
        var into = new IntoBlock<(LeftObject, RightObject)>(async (c, value) =>
        {
            var (left, right) = value;
            await c.ProduceAsync("result", left.Id, new ResultObject(left.Id, right));
        });

        left.LinkTo(join.Left, new DataflowLinkOptions() { PropagateCompletion = true });
        right.LinkTo(join.Right, new DataflowLinkOptions() { PropagateCompletion = true });
        join.LinkTo(into, new DataflowLinkOptions() { PropagateCompletion = true });
    }

    static readonly Dictionary<string, Func<Guid, (AggregateEvent<Guid>, Aggregate<Guid>?), Task<Aggregate<Guid>?>>> _functs = [];

    public static void MapAggregate(this WebApplication app)
    {
        _functs.Add("ChangeName", async (k, v) =>
        {
            var payload = await GetPayload<Guid, ChangeName>(v.Item1);

            var aggregate = v.Item2 ?? new Aggregate<Guid>(k, string.Empty) with
            {
                Name = payload.Name,
            };

            return aggregate;
        });

        app.MapStream<Guid, AggregateEvent<Guid>>("eventstream")
            .Aggregate<Guid, Aggregate<Guid>>("aggregate")
            .WithGroupId("Aggregate")
            .WithClientId("Aggregate");
    }

    public static IKafkaConventionBuilder Aggregate<TKey, TValue>(this IStreamBuilder<TKey, AggregateEvent<TKey>> builder, string topic)
        where TValue : Aggregate<TKey>
    {
        Dictionary<string, Func<TKey, (AggregateEvent<TKey>, TValue?), Task<TValue?>>> _handles = [];

        _handles.Add(nameof(ChangeName), async (k, v) =>
        {
            var payload = await GetPayload<TKey, ChangeName>(v.Item1);

            var aggregate = (v.Item2 ?? new Aggregate<TKey>(k, string.Empty)) with
            {
                Name = payload.Name,
            };

            return (TValue)aggregate;
        });


        return builder.Join<TKey, TValue>(topic).OnKey()
            .Into(async (c, k, v) =>
            {
                if (v.Item1 == null)
                    return;

                var result = await _handles[v.Item1.Name](k, (v.Item1, v.Item2));

                if (result == v.Item2)
                {
                    return;
                }

                await c.ProduceAsync(topic, k, result);
            });
    }


    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private static async Task<T> GetPayload<TKey, T>(AggregateEvent<TKey> @event)
    {
        var jsonString = JsonSerializer.Serialize(@event.Payload);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
        var result = await JsonSerializer.DeserializeAsync<T>(stream, _options);
        return result is null ? throw new InvalidCastException("Payload can not be read") : result;
    }
}

public record AggregateEvent<TKey>(TKey Id, string Name, object Payload);
public record Aggregate<TKey>(TKey Id, string Name);

public record ChangeName(string Name);
