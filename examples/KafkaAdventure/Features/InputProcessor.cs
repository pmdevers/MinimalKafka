namespace KafkaAdventure.Features;

public static class InputProcessor
{
    public static void MapInput<T>(this T app)
        where T : IEndpointRouteBuilder
    {
        Console.WriteLine("Starting Up InputFeature");

        app.MapHub<InputHub>("/input");
    }
}
