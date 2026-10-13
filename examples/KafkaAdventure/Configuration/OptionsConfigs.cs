namespace KafkaAdventure.Configuration;

public static class OptionsConfigs
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddOptionsConfigs(ILogger logger)
        {

            logger.ServicesRegistered("Configuration");
            return builder;
        }
    }
}
