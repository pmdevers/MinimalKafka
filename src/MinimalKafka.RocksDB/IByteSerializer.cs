namespace MinimalKafka.Stream.Storage.RocksDB;

/// <summary>
/// Interface for serializing and deserializing byte arrays.
/// </summary>
public interface IByteSerializer
{
    /// <summary>
    /// Serializes an object of type T to a byte array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <summary>
/// Serializes the specified value into a byte array.
/// </summary>
/// <typeparam name="T">The type of the value to serialize.</typeparam>
/// <param name="value">The value to serialize.</param>
/// <returns>A byte array representing the serialized value.</returns>
    byte[] Serialize<T>(T value);

    /// <summary>
    /// Deserializes a byte array to an object of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <summary>
/// Deserializes a byte array into an object of type <typeparamref name="T"/>.
/// </summary>
/// <param name="bytes">The byte array to deserialize, or null.</param>
/// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
    T Deserialize<T>(byte[]? bytes);
}
