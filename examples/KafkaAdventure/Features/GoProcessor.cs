using KafkaAdventure.Domain;
using MinimalKafka;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features;

public static class GoProcessor
{
    public static void MapGoProcessor<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder
    {
        builder.MapStream<Guid, AppCommand>("game-go")
            .InnerJoin<Guid, Location>("game-player-location").OnKey()
            .Into(async (c, k, v) =>
            {
                var (command, location) = v;

                if(command is null || c.TopicName == "game-player-location")
                {
                    return;
                }

                var store = c.GetTopicStore("locations");
                location ??= await store.FindByKey<int, Location>(1);
                if (location == null) 
                {
                    throw new InvalidOperationException("No locations");
                }

                if(location.Exits.TryGetValue(command.Args.First(), out int value))
                {
                    await c.ProduceAsync("game-response", k, new AppResponse(Commands.Go, $"You moved {command.Args.First()}"));
                    await c.ProduceAsync("game-player-position", k, new PlayerPosition(k, value));
                } 
                else
                {
                    await c.ProduceAsync("game-response", k, new AppResponse(Commands.Go, "You can't go that way."));
                }
            });

        builder.MapStream<int, Location>("game-locations")
            .Join<Guid, PlayerPosition>("game-player-position")
            .On((location, position) => location.Id == position.LocationId)
            .Into(async (c, r) =>
            {
                var (location, position) = r;

                await c.ProduceAsync("game-player-location", position.PlayerId, location);
            });
    }
}
