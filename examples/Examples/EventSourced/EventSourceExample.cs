using Examples.EventSourced.Generic;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka;

namespace Examples.EventSourced;

public static class EventSourceExample
{
    public static void MapEventSourcedExample<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder, IEndpointRouteBuilder
    {
        builder.MapGet("/command", async ([FromServices] IKafkaProducer producer) =>
        {
            await producer.ProduceAsync("aggregate-commands", Guid.NewGuid(), new TestCommand
            {
                Type = CommandType.Create,
            });
        });

        builder.MapEventSourced<Guid, TestState, TestCommand, CommandType, TestEvent, EventTypes>
        (
            CommandProcessor.Process,
            EventApplier.Apply
        );
    }
}
