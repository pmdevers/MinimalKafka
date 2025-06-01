namespace MinimalKafka.Stream.Storage.RocksDB;

public interface IByteSerializer
{
    byte[] Serialize<T>(T value);
    T Deserialize<T>(byte[]? bytes);
}
