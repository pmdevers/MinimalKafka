using KafkaAdventure.Domain;
using Microsoft.AspNetCore.SignalR;
using MinimalKafka;

namespace KafkaAdventure.Features;

public class InputHub(
    IKafkaProducer producer
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

        try
        {
            var command = Enum.Parse<Commands>(cmd.First(), true);
            await producer.ProduceAsync("game-commands", gameId, 
                new AppCommand(command, [.. cmd.Skip(1)]));
        } catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
    }
}
