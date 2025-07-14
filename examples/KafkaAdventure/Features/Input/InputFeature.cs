using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MinimalKafka;

namespace KafkaAdventure.Features.Input;

public static class InputFeature
{
    public static void MapInput<T>(this T app)
        where T : IEndpointRouteBuilder, IApplicationBuilder
    {
        Console.WriteLine("Starting Up InputFeature");
        app.MapHub<InputHub>("/input");
        app.MapTopic("game-response", HandleAsync);
    }

    public static async Task HandleAsync([FromServices] IHubContext<InputHub> hub, [FromKey] string gameId, [FromValue] Response response)
    {
        await hub.Clients.Group(gameId).SendAsync("ReceiveMessage", response.Value);
    }

    public record Response(string Command, string Value);
}