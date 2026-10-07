using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MinimalKafka.Serialization;

public static class SerializationBuilderExtensions
{
    /// <summary>Sets the format used for topics and produce calls that do not specify one. JSON is the initial default.</summary>
    public static IMinimalKafkaBuilder UseDefaultFormat(this IMinimalKafkaBuilder builder, string format)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        builder.Services.Configure<SerializationOptions>(options => options.DefaultFormat = format);
        return builder;
    }

    public static IMinimalKafkaBuilder ConfigureJson(
        this IMinimalKafkaBuilder builder,
        Action<System.Text.Json.JsonSerializerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        builder.Services.Configure<SerializationOptions>(options => configure(options.Json));
        return builder;
    }

    /// <summary>Adds the Avro format using Confluent Schema Registry. Types must be generated Avro classes or GenericRecord.</summary>
    public static IMinimalKafkaBuilder AddAvro(
        this IMinimalKafkaBuilder builder,
        Action<SchemaRegistryConfig>? configureRegistry = null,
        Action<AvroSerializerConfig>? configureSerializer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        AddSchemaRegistry(builder.Services, configureRegistry);

        var config = new AvroSerializerConfig();
        configureSerializer?.Invoke(config);
        builder.Services.AddSingleton<IMessageSerializer>(services =>
            new AvroMessageSerializer(services.GetRequiredService<ISchemaRegistryClient>(), config));
        return builder;
    }

    /// <summary>Adds the Protobuf format using Confluent Schema Registry. Types must be protoc generated messages.</summary>
    public static IMinimalKafkaBuilder AddProtobuf(
        this IMinimalKafkaBuilder builder,
        Action<SchemaRegistryConfig>? configureRegistry = null,
        Action<ProtobufSerializerConfig>? configureSerializer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        AddSchemaRegistry(builder.Services, configureRegistry);

        var config = new ProtobufSerializerConfig();
        configureSerializer?.Invoke(config);
        builder.Services.AddSingleton<IMessageSerializer>(services =>
            new ProtobufMessageSerializer(services.GetRequiredService<ISchemaRegistryClient>(), config));
        return builder;
    }

    /// <summary>Registers a custom format. A serializer with the same format name replaces the built-in one.</summary>
    public static IMinimalKafkaBuilder AddSerializer<TSerializer>(this IMinimalKafkaBuilder builder)
        where TSerializer : class, IMessageSerializer
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton<IMessageSerializer, TSerializer>();
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
