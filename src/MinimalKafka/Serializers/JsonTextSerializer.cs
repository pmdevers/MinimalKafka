using Confluent.Kafka;
using System.Diagnostics.Contracts;
using System.Text.Json;

namespace MinimalKafka.Serializers;

/// <summary>Initializes a new instance of the <see cref="KafkaJsonSerializer{T}"/> class.</summary>
public class JsonTextSerializer<T>(JsonSerializerOptions? jsonOptions) : ISerializer<T>, IDeserializer<T>
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
        if (isNull || typeof(T) == typeof(Ignore))
        {
            return default!;
        }

        data = data.StartsWith(UTF8.BOM) ? data[3..] : data;
        var result = JsonSerializer.Deserialize<T>(data, _jsonOptions);

        return (result ?? default)!;
    }
}

