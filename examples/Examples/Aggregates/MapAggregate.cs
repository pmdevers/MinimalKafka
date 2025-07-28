namespace Examples.Aggregates;

public static class MapAggregate
{

    public static void Map()
    {
        var builder = new AggregateBuilder<>
    }
}


public class Event : IEvent<Guid>
{
    public Guid Key { get; }
    public string Type { get; }

    public CreatedEvent? Created { get; }
}

public record CreatedEvent(string Name);

public record Agg : IAggregate<Guid, Agg, Event>
{
    public required  Guid Key { get; init; }

    public int Version { get; init; }

    public string Name { get; private set; } = string.Empty;

    public static void Configure(IAggregateBuilder<Guid, Event, Agg> builder)
    {
        builder.When(x => x.Type == "Created").Then((a, e) => a.Created(e));
    }

    private Result<Agg> Created(Event e)
    {
        if (e.Created is null)
            return Result.Failed(this, "EventBody missing.");

        return this with
        {
            Name = e.Created.Name
        };
    }

    public static Agg Create(Guid key)
        => new() { Key = key };
}