using KafkaAdventure.Extensions;
using MinimalKafka.Extension;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features.CommandProcessor;

public static class ProcessorFeature
{
    /// <summary>
    /// Configures the Kafka stream processor for handling game commands on the web application.
    /// </summary>
    /// <remarks>
    /// Sets up processing for the "game-commands" Kafka topic, routing incoming commands to appropriate topics based on their type:
    /// - "HELP" commands produce a list of available commands to "game-response".
    /// - "GO" and "LOOK" commands produce movement instructions to "game-movement".
    /// - "INVENTORY" commands are forwarded to "game-inventory".
    /// - "ECHO" commands produce an echo response to "game-response".
    /// - Unrecognized commands produce an error response to "game-response".
    /// Registers the entire pipeline as a feature named "Commands".
    /// </remarks>
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
        /// <summary>
/// Determines whether the command string starts with the specified value, ignoring case.
/// </summary>
/// <param name="s">The string to compare against the start of the command.</param>
/// <returns>True if the command starts with the specified string; otherwise, false.</returns>
public bool IsCommand(string s) => Cmd.StartsWith(s, StringComparison.InvariantCultureIgnoreCase);
    };

    public record Response(string Command, string Value);
}
