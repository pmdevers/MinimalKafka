using KafkaAdventure.Domain;
using MinimalKafka;

namespace KafkaAdventure.Features;

public static class HelpProcessor
{
    public static void MapHelp<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder
    {
        builder.MapTopic("game-help", async (KafkaContext context, Guid key, AppCommand value) =>
        {
            await context.ProduceAsync("game-response", key,
                new AppResponse(value.Command, "Commands: go [north/south/east/west], look, take [item], inventory"));
        });
    }
}
