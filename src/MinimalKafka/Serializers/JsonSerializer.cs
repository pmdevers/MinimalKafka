using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MinimalKafka.Serializers;
internal class JsonSerializer<T> : ISerializer<T>, IDeserializer<T>
{
    public JsonSerializer(JsonSerializerOptions options)
    {
        Options = options;
    }

    public JsonSerializerOptions Options { get; }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<T>(data, Options);
    }

    public byte[] Serialize(T data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, Options);
    }
}
