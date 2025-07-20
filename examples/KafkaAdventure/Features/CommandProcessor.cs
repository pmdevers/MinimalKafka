using KafkaAdventure.Domain;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features;

public static class CommandProcessor
{
    public static void MapCommand<TBuilder>(this TBuilder builder)
        where TBuilder : IApplicationBuilder
    {
        builder.MapStream<Guid, AppCommand>("game-commands")
            .SplitInto(x =>
            {
                x.Branch((k, v) => v.Command == Commands.Help).To("game-help");
                x.Branch((k, v) => v.Command == Commands.Go).To("game-go");
                x.Branch((k, v) => v.Command == Commands.Look).To("game-look");
                x.Branch((k, v) => v.Command == Commands.Inventory).To("game-inventory");
                x.Branch((k, v) => v.Command == Commands.Echo).To("game-echo");
                x.DefaultBranch("game-unknown");
            });
    }
}
