using System.Text.Json;

namespace MinimalKafka.Serializers;
internal class SystemTextJsonSerializer<T> : IKafkaSerializer<T>
{
    public T? Deserialize(byte[] value, bool isNull)
    {
        return JsonSerializer.Deserialize<T>(value);
    }

    public byte[] Serialize(T value)
    {
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, value, typeof(T));
        return stream.ToArray();
    }
}
