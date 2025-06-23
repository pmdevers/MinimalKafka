using System.Text.Json;

namespace MinimalKafka.Stream.Storage.RocksDB;

internal class ByteSerializer : IByteSerializer
{
    /// <summary>
    /// Serializes the specified object to a UTF-8 encoded JSON byte array.
    /// </summary>
    /// <param name="value">The object to serialize. Must not be null.</param>
    /// <returns>A byte array containing the UTF-8 encoded JSON representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public byte[] Serialize<T>(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return JsonSerializer.SerializeToUtf8Bytes(value);
    }

    /// <summary>
    /// Deserializes a UTF-8 encoded JSON byte array into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="bytes">The byte array containing the JSON data to deserialize.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if deserialization fails and returns null.</exception>
    public T Deserialize<T>(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            throw new ArgumentNullException(nameof(bytes));

        return JsonSerializer.Deserialize<T>(bytes) ?? throw new InvalidOperationException("Deserialization failed");
    }
}