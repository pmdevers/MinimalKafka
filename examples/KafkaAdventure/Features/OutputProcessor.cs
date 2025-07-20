using KafkaAdventure.Domain;
using Microsoft.AspNetCore.SignalR;
using MinimalKafka.Stream;

namespace KafkaAdventure.Features;

public static class OutputProcessor
{
    public static void MapOutput<T>(this T app)
        where T: IApplicationBuilder
    {
        app.MapStream<Guid, AppResponse>("game-response")
            .Into(async (c, k, v) =>
            {
                var hub = c.RequestServices.GetRequiredService<IHubContext<InputHub>>();
                await hub.Clients.Group(k.ToString()).SendAsync("ReceiveMessage", v.Reponse);
            });
    }
}