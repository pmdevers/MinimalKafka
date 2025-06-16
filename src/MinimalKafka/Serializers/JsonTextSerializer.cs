using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalKafka.Builders;
using MinimalKafka.Extension;
using System.Diagnostics.Contracts;
using System.Text.Json;

namespace MinimalKafka.Serializers;


public static class AddKafkaBuilderExtensions
{
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

/// <summary>Initializes a new instance of the <see cref="KafkaJsonSerializer{T}"/> class.</summary>
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