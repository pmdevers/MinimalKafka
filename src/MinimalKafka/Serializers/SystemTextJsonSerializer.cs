using System;
using System.Text.Json;

namespace MinimalKafka.Serializers;
internal class SystemTextJsonSerializer<T> : IKafkaSerializer<T>
{
    private readonly JsonSerializerOptions _jsonOptions = 
        new(JsonSerializerDefaults.Web);

    public T Deserialize(ReadOnlySpan<byte> value)
    {
        if (value.Length >= 3 && value[..3].SequenceEqual(Utf8Constants.BOM))
            value = value[3..];

        if(value.Length == 0)
        {
            return default!;
        }

        var result = JsonSerializer.Deserialize<T>(value, _jsonOptions);

        return (result ?? default)!;
    }

    public byte[] Serialize(T value)
    {
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, value, typeof(T));
        return stream.ToArray();
    }
}
