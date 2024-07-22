using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Contracts;
using System.Text.Json;

namespace Pmdevers.MinimalKafka.Serializers;

/// <summary>Initializes a new instance of the <see cref="KafkaJsonSerializer{T}"/> class.</summary>
public class JsonTextSerializer<T>(
    JsonSerializerOptions? jsonOptions,
    ILogger<JsonTextSerializer<T>> logger) : ISerializer<T>, IDeserializer<T>
{
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions
            ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly ILogger _logger = logger;

    /// <inheritdoc />
    [Pure]
    public byte[] Serialize(T? data, SerializationContext context)
        => JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions);

    /// <inheritdoc />
    [Pure]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "<Pending>")]
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        try
        {
            if (isNull || typeof(T) == typeof(Ignore))
            {
                return default!;
            }

            data = data.StartsWith(UTF8.BOM) ? data[3..] : data;
            var result = JsonSerializer.Deserialize<T>(data, _jsonOptions);

            return (result ?? default)!;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize: '{Message}'.", ex.Message);
            throw;
        }
    }
}

