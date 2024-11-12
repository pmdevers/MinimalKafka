using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;
using MinimalKafka.Stream;
using System.Threading.Tasks.Dataflow;
using MinimalKafka;
using System.Text.Json;
using System.Text;
using MinimalKafka.Extension;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

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

    public static void MapAggregate(this WebApplication app)
    {
        app.MapStream<Guid, AggregateEvent<Guid>>("eventstream")
            .Aggregate<Guid, MyAggregate>("aggregate", builder =>
            {
                builder.AddEvent<ChangeName>((v, e) => v with { Name = e.Name });
                builder.AddEvent<ChangeSurName>((v, e) => v with { SurName = e.Surname });
            })
            .WithGroupId("Aggregate")
        .WithClientId("Aggregate");
    }
}

public record MyAggregate(Guid Id) : Aggregate<Guid>(Id), IAggregate<Guid, MyAggregate>
{
    public static MyAggregate Create(KafkaContext context, Guid key)
    {
        return new MyAggregate(key);
    }
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
}

public record ChangeName(string Name);
public record ChangeSurName(string Surname);
