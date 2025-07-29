using Examples.Aggregates.Applier;
using Examples.Aggregates.Commander;
using Examples.Aggregates.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka;

namespace Examples.Aggregates;

public static class MapAggregate
{
    public record CreateRequest(string Name);
    public static void MapAggregate2<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder, IEndpointRouteBuilder
    {
        builder.MapPost("/command", async (
            [FromServices] IKafkaProducer producer,
            [FromBody] CreateRequest request) =>
        {
            var cmd = new Command() 
            { 
                Type = CommandTypes.Create, 
                Create = new(request.Name)
            };
            await producer.ProduceAsync("commands", cmd.Key, cmd);
        });

        builder.MapCommander<AggCommander, Guid, Agg, Command, Event>("commands", "events");
        builder.MapApplier<AggApplier, Guid, Agg, Event>("events");

        builder.MapGet("/test/{id}", GetAggregate);
    }

    private static async Task<Results<Ok<Agg>, NotFound<Guid>>> GetAggregate(
            [FromServices] IAggregateStore<Guid, Agg> store,
            [FromRoute(Name = "id")] Guid id)
        {
            var result = await store.FindByIdAsync(id);

            if(result is null)
            {
                return TypedResults.NotFound(id);
            }

            return TypedResults.Ok(result);
        }
}

public enum EventTypes
{
    Created,
    Deleted
}

public class Event : ITypedEvent<Guid, EventTypes>
{
    public Guid Key { get; set; }
    public EventTypes Type { get; set; }
    public CreatedEvent? Created { get; set; }
}

public record CreatedEvent(string Name);

public class AggApplier : IEventApplier<Guid, Agg, Event>
{
    public static void Configure(IApplierBuilder<Guid, Agg, Event> builder)
    {
        builder
            .When(EventTypes.Created).Then((a, e) => a.Created(e))
            .When(EventTypes.Deleted).Then((s, e) => s.Deleted(e));
    }
}

public enum CommandTypes
{
    Create,
    Delete
}

public class Command : ITypedCommand<Guid, CommandTypes>
{
    public Guid Key { get; init; } = Guid.NewGuid();
    public required CommandTypes Type { get; init; }
    public CreateCommand? Create { get; set; }
}

public record CreateCommand(string Name);

public class AggCommander : ICommander<Guid, Agg, Command, Event>
{
    public static void Configure(ICommanderBuilder<Guid, Agg, Command, Event> builder)
    {
        builder.When(CommandTypes.Create).Then((s, c) => s.Create(c));
    }
}

public record Agg() : IAggregate<Guid>
{
    public Guid Key { get; init; }

    public int Version { get; init; }

    public string Name { get; private set; } = string.Empty;

    internal Result<Agg> Deleted(Event e)
    {
        throw new NotImplementedException();
    }

    internal Result<Agg> Created(Event e)
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

    internal Result<Event[]> Create(Command c)
    {
        if(c.Create is null)
        {
            return Result.Failed(Array.Empty<Event>(), "Command");
        }
        var @event = new Event()
        {
            Key = Key,
            Type = EventTypes.Created,
            Created = new CreatedEvent(c.Create.Name)
        };

        return new Event[] { @event };
    }
}