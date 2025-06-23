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
    /// <returns></returns>
    byte[] Serialize<T>(T value);

    /// <summary>
    /// Deserializes a byte array to an object of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    T Deserialize<T>(byte[]? bytes);
}
