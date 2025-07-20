using KafkaAdventure.Domain;
using MinimalKafka;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features;

public static class LookProccessor
{
    public static void MapLook<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder
    {
        builder.MapStream<Guid, AppCommand>("game-look")
           .Join<Guid, Location>("game-player-location").OnKey()
           .Into(async (c, k, v) =>
           {
               var (command, location) = v;

               if (command is null)
               {
                   return;
               }

               if(location is null)
               {
                   // this is a new game
                   await c.ProduceAsync("game-player-position", k, new PlayerPosition(k, 1));
                   return;
               }

               await c.ProduceAsync("game-response", k, 
                   new AppResponse(command.Command, $"{location.Description}"));
               
               var store = c.GetTopicStore("locations");
               foreach(var exit in location.Exits)
               {
                   var exitLocation = await store.FindByKey<int, Location>(exit.Value);
                   await c.ProduceAsync("game-response", k,
                        new AppResponse(command.Command, $"[{exit.Key}] - {exitLocation?.Description}"));
               }
           });
    }
}
