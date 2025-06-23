using Confluent.Kafka;
using KafkaAdventure.Extensions;
using Microsoft.AspNetCore.Mvc;
using MinimalKafka.Extension;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features.Locations;

public static class LocationsFeature
{
    /// <summary>
    /// Configures HTTP and Kafka stream endpoints for managing location data.
    /// </summary>
    /// <remarks>
    /// Maps a POST endpoint at <c>/locations</c> that accepts an array of <see cref="Location"/> objects and produces them to the "game-locations" Kafka topic. Also sets up a Kafka stream consumer for the "game-locations" topic, updating or adding locations in the <see cref="LocationContext"/> based on incoming messages.
    /// </remarks>
    public static void MapLocations<T>(this T app)
        where T : IEndpointRouteBuilder, IApplicationBuilder
    {
        Console.WriteLine("Starting Up LocationsFeature");

        app.MapPost("/locations", async (
            [FromServices] IProducer<int, Location> producer, 
            [FromBody] Location[] locations) =>
        {
            foreach(var location in locations) {    
                await producer.ProduceAsync("game-locations", new() {
                    Key = location.Id,
                    Value = location
                });
            }
        });

        app.MapStream<int, Location>("game-locations")
            .Into((c, k, v) =>
            {
                var context = c.RequestServices.GetRequiredService<LocationContext>();
                var existing = context.Locations.FirstOrDefault(x => x.Id == k);

                if (existing == null) 
                {
                    v.Id = k;
                    context.Locations.Add(v);
                }
                else 
                {
                    existing.Exits = v.Exits;
                    existing.Description = v.Description;
                }

                return Task.CompletedTask;
            })
            .WithOffsetReset(AutoOffsetReset.Earliest)
            .AsFeature("Locationstest");
    }
}
 