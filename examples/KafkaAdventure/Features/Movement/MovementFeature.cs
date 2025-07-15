using KafkaAdventure.Features.Locations;
using MinimalKafka;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features.Movement;

public static class MovementFeature
{
    public static void MapMovement<T>(this T app)
        where T : IEndpointRouteBuilder, IApplicationBuilder
    {
        Console.WriteLine("Starting Up MovementFeature");

        app.MapStream<string, Movement>("game-movement")
            .Join<string, Location>("game-player-position").OnKey()
            .Into(async (c, k, v) =>
            {
                if (v.Item1 is null || c.TopicName == "game-player-position")
                {
                    return;
                }

                var context = c.RequestServices.GetRequiredService<LocationContext>();
                var locations = context.Locations;

                v.Item2 ??= locations.Single(x => x.Id == 1);

                if (v.Item2.Exits.ContainsKey(v.Item1.Direction.ToLower()))
                {
                    var newLocation = v.Item2.Exits[v.Item1.Direction.ToLower()];
                    var newPosition = locations.Single(x => x.Name == newLocation);
                    await c.ProduceAsync("game-response", k, new Response(v.Item1.Cmd, $"You moved {v.Item1.Direction.ToLower()}"));
                    await c.ProduceAsync("game-player-position", k, newPosition);
                } 
                else if(v.Item1.Direction == "LOOK")
                {
                    await c.ProduceAsync("game-response", k, new Response("LOCATION", $"{v.Item2.Description}"));
                }
                else
                {
                    await c.ProduceAsync("game-response", k, new Response("LOCATION", $"You can't go that way."));
                }
            });
    }
    public record Position(int X, int Y);

    public record class Movement(string Cmd, string Direction)
    {
        public bool IsCommand(string s) => Cmd.StartsWith(s, StringComparison.InvariantCultureIgnoreCase);
    };

    public record Response(string Command, string Value);
}
