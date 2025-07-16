using MinimalKafka.Internals;
using System.Text;
using System.Text.Json;

namespace MinimalKafka.Serializers;

internal class SystemTextJsonSerializer<T>(JsonSerializerOptions? options = null) : IKafkaSerializer<T>
{
    private readonly JsonSerializerOptions _jsonOptions = options ??
        new(JsonSerializerDefaults.Web);

    public T Deserialize(ReadOnlySpan<byte> value)
    {
        if (value.Length >= 3 && value[..3].SequenceEqual(Utf8Constants.BOM))
            value = value[3..];

        if(value.Length == 0)
        {
            return default!;
        }

        try
        {
            // Attempt to deserialize directly from the span
            return JsonSerializer.Deserialize<T>(value, _jsonOptions) ?? default!;
        }
        catch (JsonException ex)
        {
            // If deserialization fails, try to convert to string first
            throw new KafkaProcesException(ex, $"Failed to deserialize value: '{Encoding.UTF8.GetString(value)}' to '{typeof(T).FullName}'");
        }
    }

    public byte[] Serialize(T value)
    {
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, value, typeof(T));
        return stream.ToArray();
    }
}
