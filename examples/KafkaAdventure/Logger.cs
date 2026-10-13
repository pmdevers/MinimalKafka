namespace KafkaAdventure;

public static partial class Logger
{
    private const int Configuration = 1000;
    private const int Domain = 2000;
    private const int Features = 3000;
    private const int Infrastructure = 4000;

    [LoggerMessage(
        EventId = Configuration + 1,
        Level = LogLevel.Information,
        Message = "{Project} services registered")]
    public static partial void ServicesRegistered(this ILogger logger, string project);

    [LoggerMessage(
        EventId = Configuration + 2,
        Level = LogLevel.Information,
        Message = "{Project} were configured")]
    public static partial void OptionsConfigured(this ILogger logger, string project);
}
