using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MinimalKafka.Serialization;

/// <summary>Extension methods for configuring message serialization formats.</summary>
public static class SerializationBuilderExtensions
{
    extension(IMinimalKafkaBuilder builder)
    {
        /// <summary>Sets the format used for topics and produce calls that do not specify one. JSON is the initial default.</summary>
        public IMinimalKafkaBuilder WithDefaultFormat(string format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(format);
            builder.Services.Configure<SerializationOptions>(options => options.DefaultFormat = format);
            return builder;
        }

        /// <summary>Configures the JSON serializer options used for the JSON message format.</summary>
        public IMinimalKafkaBuilder WithJsonSerializer(
            Action<SchemaRegistryConfig>? configureRegistry = null,
            Action<JsonSerializerOptions>? configureSerializer = null
        )
        {
            AddSchemaRegistry(builder.Services, configureRegistry);

            ArgumentNullException.ThrowIfNull(configureSerializer);

            builder.Services.Configure<SerializationOptions>(options => configureSerializer?.Invoke(options.Json));
            return builder;
        }

        /// <summary>Adds the Avro format using Confluent Schema Registry. Types must be generated Avro classes or GenericRecord.</summary>
        public IMinimalKafkaBuilder WithAvroSerializer(
            Action<SchemaRegistryConfig>? configureRegistry = null,
            Action<AvroSerializerConfig>? configureSerializer = null)
        {
            AddSchemaRegistry(builder.Services, configureRegistry);

            ArgumentNullException.ThrowIfNull(configureSerializer);

            var config = new AvroSerializerConfig();
            configureSerializer?.Invoke(config);
            builder.Services.AddSingleton<IKafkaSerializer>(services =>
                new AvroMessageSerializer(services.GetRequiredService<ISchemaRegistryClient>(), config));
            return builder;
        }

        /// <summary>Adds the Protobuf format using Confluent Schema Registry. Types must be protoc generated messages.</summary>
        public IMinimalKafkaBuilder WithProtobufSerializer(
            Action<SchemaRegistryConfig>? configureRegistry = null,
            Action<ProtobufSerializerConfig>? configureSerializer = null)
        {
            AddSchemaRegistry(builder.Services, configureRegistry);

            var config = new ProtobufSerializerConfig();
            configureSerializer?.Invoke(config);
            builder.Services.AddSingleton<IKafkaSerializer>(services =>
                new ProtobufMessageSerializer(services.GetRequiredService<ISchemaRegistryClient>(), config));
            return builder;
        }

        /// <summary>Registers a custom format. A serializer with the same format name replaces the built-in one.</summary>
        public IMinimalKafkaBuilder AddSerializer<TSerializer>()
            where TSerializer : class, IKafkaSerializer
        {
            builder.Services.AddSingleton<IKafkaSerializer, TSerializer>();
            return builder;
        }

        // One client is shared by Avro and Protobuf, so the registry settings are additive across both calls.
        private static void AddSchemaRegistry(IServiceCollection services, Action<SchemaRegistryConfig>? configure)
        {
            var options = services.AddOptions<SchemaRegistryConfig>();
            if (configure is not null)
            {
                options.Configure(configure);
            }
            services.TryAddSingleton<ISchemaRegistryClient>(provider =>
                new CachedSchemaRegistryClient(provider.GetRequiredService<IOptions<SchemaRegistryConfig>>().Value));
        }

    }
}
