using KafkaAdventure.Domain;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka;

namespace KafkaAdventure.Features;

public static class LocationApiEndpoints
{
    public static void MapLocationsApi<T>(this T app)
        where T : IEndpointRouteBuilder, IApplicationBuilder
    {
        Console.WriteLine("Starting Up LocationsFeature");

        app.MapPost("/locations", async (
            [FromServices] IKafkaProducer producer,
            [FromBody] Location[] locations) =>
        {
            foreach (var location in locations)
            {
                await producer.ProduceAsync("game-locations", location.Id, location);
            }
        });
    }
}
