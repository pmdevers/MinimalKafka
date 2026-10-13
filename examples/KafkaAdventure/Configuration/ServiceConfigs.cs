using Confluent.Kafka;
using Microsoft.AspNetCore.ResponseCompression;
using MinimalKafka;
using MinimalKafka.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KafkaAdventure.Configuration;

public static class ServiceConfigs
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddServiceConfigs(ILogger logger)
        {
            var services = builder.Services;

            services.AddSignalR();
            services.AddMemoryCache();
            services.AddHealthChecks();
            services.AddResponseCompression(options =>
            {
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    ["application/octet-stream"]);
            });

            services.AddMinimalKafka(config =>
            {
                config.WithConfiguration(builder.Configuration.GetSection("Kafka"))
                    .WithTopicNaming(options =>
                    {
                        options.Suffix = $"-{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower()}";
                    })
                    .WithGroupId(AppDomain.CurrentDomain.FriendlyName + "-test")
                    .WithAutoOffsetReset(AutoOffsetReset.Earliest)
                    .WithJsonSerializer(configureSerializer: serializerOptions =>
                    {
                        serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                        serializerOptions.Converters.Add(new JsonStringEnumConverter());
                    })
                    .WithConsumerDurationLogging()
                    .WithRocksDB();
            });

            logger.ServicesRegistered("Services");

            return builder;
        }
    }
}
