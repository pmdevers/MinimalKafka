using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;

namespace KafkaAdventure.Features;

public class InputHub(
    IProducer<string, Response> response,
    IProducer<string, Command> command
) : Hub
{
    /// <summary>
    /// Adds the current connection to the specified game group and sends introductory messages to all clients in that group.
    /// </summary>
    /// <param name="gameId">The identifier of the game group to join.</param>
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

        await Clients.Group(gameId).SendAsync("ReceiveMessage", "Welcome to 'a Kafka Adventure'");
        await Clients.Group(gameId).SendAsync("ReceiveMessage", "You are an aspiring adventurer in search of the legendary relic.");
        await Clients.Group(gameId).SendAsync("ReceiveMessage", "Type your commands to explore the world. Type 'help' for a list of commands.");
    }

    /// <summary>
    /// Processes a client message by parsing it into a command and arguments, then produces a Command message to the "game-commands" Kafka topic for the specified game.
    /// </summary>
    /// <param name="gameId">The identifier of the game to which the command applies.</param>
    /// <param name="message">The message from the client, expected to contain a command and optional arguments separated by spaces.</param>
    public async Task SendMessage(string gameId, string message)
    {
        if(string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var cmd = message.Split(' ');
        await command.ProduceAsync("game-commands", new()
        {
            Key = gameId,
            Value = new Command(cmd.First(), cmd.Skip(1).ToArray())
        });
    }
}
public record Response(string Command, string Value);
public record Command(string cmd, string[] Args);
