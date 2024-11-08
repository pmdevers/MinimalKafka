using MinimalKafka;
using MinimalKafka.Metadata;
using MinimalKafka.Stream;

namespace Examples;

public static class LeftJoin
{
    private static bool _innerJoin;
    private readonly static IStreamStore<Guid, LeftObject> _leftStore = new InMemoryStore<Guid, LeftObject>();
    private readonly static IStreamStore<Guid, RightObject> _rightStore = new InMemoryStore<Guid, RightObject>();

    public static void MapStream(this WebApplication app, bool innerJoin = true)
    {
        _innerJoin = innerJoin;
        app.MapTopic("left", HandleLeftAsync);
        app.MapTopic("right", HandleRightAsync);
    }

    public static async Task HandleLeftAsync(KafkaContext context, [FromKey] Guid key, [FromValue] LeftObject value)
    {
        var left = await _leftStore.AddOrUpdate(key, (k) => value, (k, v) => value);
        var right = await _rightStore.FindByIdAsync(value.RightObjectId);
        await ProcessAsync(context, new Tuple<LeftObject, RightObject?>(left, right));
    }

    public static async Task HandleRightAsync(KafkaContext context, [FromKey] Guid key, [FromValue] RightObject value)
    {
        var right = await _rightStore.AddOrUpdate(key, (k) => value, (k, v) => value);
        await foreach(var item in _leftStore.FindAsync(x => x.RightObjectId == value.Id))
        {
            await ProcessAsync(context, new Tuple<LeftObject, RightObject?>(item, right));
        }
    }

    public static async Task ProcessAsync(KafkaContext context, Tuple<LeftObject, RightObject?> value)
    {
        if (value.Item2 == null && _innerJoin)
            return;

        var result = new ResultObject(value.Item1.Id, value.Item2);

        await context.Produce<Guid, ResultObject>("result", new()
        {
            Key = value.Item1.Id,
            Value = result
        });
    }
}





