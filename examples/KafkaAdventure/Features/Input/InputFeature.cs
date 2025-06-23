using KafkaAdventure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MinimalKafka;
using MinimalKafka.Metadata;

namespace KafkaAdventure.Features.Input;

public static class InputFeature
{
    /// <summary>
    /// Configures the application to map the InputHub SignalR hub and the "game-response" Kafka topic for input handling.
    /// </summary>
    /// <typeparam name="T">The application type implementing both IEndpointRouteBuilder and IApplicationBuilder.</typeparam>
    public static void MapInput<T>(this T app)
        where T : IEndpointRouteBuilder, IApplicationBuilder
    {
        Console.WriteLine("Starting Up InputFeature");
        app.MapHub<InputHub>("/input");
        app.MapTopic("game-response", HandleAsync)
            .AsFeature("Input");
    }

    /// <summary>
    /// Sends a response message to all SignalR clients in the specified game group.
    /// </summary>
    /// <param name="gameId">The identifier of the SignalR group representing the game session.</param>
    /// <param name="response">The response containing the message to send to clients.</param>
    public static async Task HandleAsync([FromServices] IHubContext<InputHub> hub, [FromKey] string gameId, [FromValue] Response response)
    {
        await hub.Clients.Group(gameId).SendAsync("ReceiveMessage", response.Value);
    }

    public record Response(string Command, string Value);
}