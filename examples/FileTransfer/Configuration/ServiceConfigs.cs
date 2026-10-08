using Confluent.Kafka;
using MinimalKafka;
using MinimalKafka.Serialization;
using System.Text.Json.Serialization;

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
            services.AddHealthChecks();
            services.AddAntiforgery();

            builder.Services.AddMinimalKafka(config =>
            {
                config
                    .WithConfiguration(builder.Configuration.GetSection("Kafka"))
                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                    .WithPartitionsAssignedHandler((_, p) => p.Select(tp => new TopicPartitionOffset(tp, Offset.Beginning)))
                    .WithJsonSerializer(
                        registry =>
                        {
                            registry.Url = "http://localhost:8081";
                        },
                        configureSerializer: x =>
                        {
                            x.Converters.Add(new JsonStringEnumConverter());
                        }
                    );
            });

            logger.ServicesRegistered("Configuration");

            return builder;
        }
    }
}