using Confluent.Kafka;
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

            services.AddMinimalKafka(config =>
                {
                    config
                        .WithConfiguration(builder.Configuration.GetSection("Kafka"))
                        .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                        .WithPartitionsAssignedHandler((_, p) => p.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning)))
                        .WithJsonSerializer(configureSerializer: x =>
                        {
                            x.Converters.Add(new JsonStringEnumConverter());
                        });
                });

            logger.ServicesRegistered("Configuration");

            return builder;
        }
    }
}