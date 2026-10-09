using Confluent.Kafka;
using Examples.Infrastructure.ClaimCheck;
using MinimalKafka;
using MinimalKafka.Serialization;
using System.Text.Json.Serialization;

namespace Examples.Configuration;

public static class ServiceConfigs
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddServiceConfigs(ILogger logger)
        {
            var services = builder.Services;

            services.AddOptions();
            services.AddOpenApi();
            services.AddHealthChecks();
            services.AddAntiforgery();
            services.AddClaimCheckStorage(builder.Configuration);

            services.AddMinimalKafka(config =>
            {
                config
                    .WithConfiguration(builder.Configuration.GetSection("Kafka"))
                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                    .WithJsonSerializer(configureSerializer: x =>
                    {
                        x.Converters.Add(new JsonStringEnumConverter());
                    })
                    .WithClaimCheck();
            });

            logger.ServicesRegistered("Configuration");

            return builder;
        }
    }
}