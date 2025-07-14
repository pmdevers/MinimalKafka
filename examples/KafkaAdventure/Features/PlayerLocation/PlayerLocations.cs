using KafkaAdventure.Features.Input;
using KafkaAdventure.Features.Locations;
using MinimalKafka;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features.PlayerLocation;

public static class PlayerLocations
{
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
                              
            });
    }
}
