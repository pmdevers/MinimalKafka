using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Extension;
using System.Diagnostics.Contracts;
using System.Text.Json;

namespace MinimalKafka.Serializers;

/// <summary>
/// Extension Methods for <see cref="IAddKafkaBuilder"/> .
/// </summary>
public static class AddKafkaBuilderExtensions
{
    /// <summary>
    /// Configures the specified <see cref="IAddKafkaBuilder"/> to use JSON-based serializers and deserializers for
    /// Kafka message keys and values.
    /// </summary>
    /// <remarks>This method registers JSON serializers and deserializers for both keys and values in Kafka
    /// messages. The serializers use the <see cref="JsonSerializerOptions"/> provided via the <paramref
    /// name="options"/> parameter, or default to <see cref="JsonSerializerDefaults.Web"/> if no options are
    /// specified.</remarks>
    /// <param name="builder">The <see cref="IAddKafkaBuilder"/> to configure.</param>
    /// <param name="options">An optional action to configure the <see cref="JsonSerializerOptions"/> used by the JSON serializers. If not
    /// provided, the default options for <see cref="JsonSerializerDefaults.Web"/> will be used.</param>
    /// <returns>The configured <see cref="IAddKafkaBuilder"/> instance, allowing for further chaining of configuration methods.</returns>
    public static IAddKafkaBuilder WithJsonSerializers(this IAddKafkaBuilder builder, Action<JsonSerializerOptions>? options = null)
    {
        var defaults = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options?.Invoke(defaults);
        builder.Services.AddSingleton(defaults);
        builder.Services.AddTransient(typeof(JsonTextSerializer<>));

        builder
           .WithKeyDeserializer(typeof(JsonTextSerializer<>))
           .WithValueDeserializer(typeof(JsonTextSerializer<>))
           .WithKeySerializer(typeof(JsonTextSerializer<>))
           .WithValueSerializer(typeof(JsonTextSerializer<>));

        return builder;
    }
}

/// <summary>Initializes a new instance of the <see cref="JsonTextSerializer{T}"/> class.</summary>
internal sealed class JsonTextSerializer<T>(JsonSerializerOptions? jsonOptions) : ISerializer<T>, IDeserializer<T>
{
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions
            ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    [Pure]
    public byte[] Serialize(T? data, SerializationContext context)
        => JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions);

    /// <inheritdoc />
    [Pure]
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || typeof(T) == typeof(Ignore) || data.IsEmpty)
        {
            return default!;
        }

        if (data.Length >= 3 && data[..3].SequenceEqual(Utf8Constants.BOM))
            data = data[3..];

        var result = JsonSerializer.Deserialize<T>(data, _jsonOptions);

        return (result ?? default)!;
    }
}