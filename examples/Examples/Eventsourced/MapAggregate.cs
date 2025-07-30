using Examples.Eventsourced.Commands;
using Examples.Eventsourced.Events;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka;
using MinimalKafka.Aggregates;
using MinimalKafka.Aggregates.Storage;

namespace Examples.Eventsourced;

public static class MapAggregate
{
    public record CreateRequest(string Name);
    public static void MapEventSourced<TBuilder>(this TBuilder builder)
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
            await producer.ProduceAsync("commands", cmd.StreamId, cmd);
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
