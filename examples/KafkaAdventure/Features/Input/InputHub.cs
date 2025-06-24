using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;

namespace KafkaAdventure.Features;

public class InputHub(
    IProducer<string, Response> response,
    IProducer<string, Command> command
) : Hub
{
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

        await Clients.Group(gameId).SendAsync("ReceiveMessage", "Welcome to 'a Kafka Adventure'");
        await Clients.Group(gameId).SendAsync("ReceiveMessage", "You are an aspiring adventurer in search of the legendary relic.");
        await Clients.Group(gameId).SendAsync("ReceiveMessage", "Type your commands to explore the world. Type 'help' for a list of commands.");
    }

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
            Value = new Command(cmd.First(), [.. cmd.Skip(1)])
        });
    }
}
public record Response(string Command, string Value);
public record Command(string cmd, string[] Args);
