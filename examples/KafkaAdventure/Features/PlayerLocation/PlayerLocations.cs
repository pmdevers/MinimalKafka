using KafkaAdventure.Extensions;
using KafkaAdventure.Features.Locations;
using MinimalKafka.Extension;
using MinimalKafka.Metadata;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features.PlayerLocation;

public static class PlayerLocations
{
    /// <summary>
    /// Configures a Kafka stream processing pipeline that listens for player location updates and produces descriptive responses about the player's current location and available exits.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IApplicationBuilder"/>.</typeparam>
    public static void MapPlayerLocations<T>(this T app)
        where T : IApplicationBuilder
    {

        app.MapStream<string, Location>("game-player-position")
            .Into(async (c, k, v) =>
            {
                await c.ProduceAsync("game-response", k, new Response("LOCATION", $"Your are at {v.Name}"));
                await c.ProduceAsync("game-response", k, new Response("LOCATION", $"{v.Description}"));
                
                await c.ProduceAsync("game-response", k, new Response("GO", $"The following directions lead to"));

                foreach (var x in v.Exits)
                {
                    await c.ProduceAsync("game-response", k, new Response("GO", $"{x.Key} : {x.Value}"));
                }
                              
            })
            .AsFeature("PlayerLocations");
    }
}
