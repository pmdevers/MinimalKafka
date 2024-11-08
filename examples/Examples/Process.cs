using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;
using MinimalKafka.Stream;
using System.Threading.Tasks.Dataflow;
using MinimalKafka;

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
}
