using MinimalKafka;

namespace FileTransfer.Configuration;

public static class ServiceConfigs
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddServiceConfigs(ILogger logger)
        {
            var services = builder.Services;

            services.AddOptions();
            services.AddOpenApi();

            services.AddMinimalKafka(config =>
                {
                    config.WithConfiguration(builder.Configuration.GetSection("Kafka"));

                    config.WithJsonSerializers(x =>
                    {
                        x.PropertyNameCaseInsensitive = true;
                    });
                    config.WithInMemoryStore();
                });

            logger.ServicesRegistered("Configuration");

            return builder;
        }
    }
}