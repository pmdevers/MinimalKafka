using System.Text.Json;

namespace MinimalKafka.Stream.Storage.RocksDB;

internal class ByteSerializer : IByteSerializer
{
    public byte[] Serialize<T>(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return JsonSerializer.SerializeToUtf8Bytes(value);
    }

    public T Deserialize<T>(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentNullException(nameof(bytes));

        return JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidOperationException("Deserialization failed");
    }
}