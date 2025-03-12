using KafkaAdventure.Extensions;
using MinimalKafka;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features.CommandProcessor;

public static class ProcessorFeature
{
    public static void MapProcessor(this WebApplication app)
    {
        Console.WriteLine("Starting Up ProcessorFeature");

        app.MapStream<string, Command>("game-commands")
            .SplitInto(x =>
            {
                x.Branch(
                    (k, v) => v.IsCommand("HELP"),
                    (c,k,v) => c.ProduceAsync("game-response", k, 
                    new Response(v.Cmd, "Commands: go [north/south/east/west], look, take [item], inventory"))
                );

                x.Branch(
                    (k, v) => v.IsCommand("GO"),
                    (c, k, v) => c.ProduceAsync("game-movement", k, 
                    new { v.Cmd, Direction = string.Join("", v.Args) })
                );

                x.Branch(
                    (k, v) => v.IsCommand("LOOK"),
                    (c, k, v) => c.ProduceAsync("game-movement", k,
                        new { Cmd = "GO", Direction = "LOOK" }
                    )
                );

                x.Branch(
                    (k, v) => v.IsCommand("INVENTORY"),
                    (c, k, v) => c.ProduceAsync("game-inventory", k, v)
                );

                x.Branch(
                    (k, v) => v.IsCommand("ECHO"), 
                    (c, k, v) => c.ProduceAsync("game-response", k, 
                    new Response(v.Cmd, $"ECHO: {string.Join(' ', v.Args)}"))
                );

                x.DefaultBranch((c, k, v) 
                    => c.ProduceAsync("game-response", k, 
                    new Response(v.Cmd, $"The command '{v.Cmd}' is invalid!")));
            })
            .AsFeature("Commands");
    }

    public record Command(string Cmd, string[] Args)
    {
        public bool IsCommand(string s) => Cmd.StartsWith(s, StringComparison.InvariantCultureIgnoreCase);
    };

    public record Response(string Command, string Value);
}
