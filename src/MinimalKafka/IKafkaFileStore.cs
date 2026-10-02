namespace MinimalKafka;

/// <summary>
/// Describes a class that can store kafka files.
/// </summary>
public interface IKafkaFileStore
{
    /// <summary>
    /// Stores a KafkaFile in the Store.
    /// </summary>
    /// <returns></returns>
    Task StoreAsync(Guid key, ReadOnlyMemory<byte> data);

    /// <summary>
    /// Loads the Data of the KafkaFile from the store.
    /// </summary>
    /// <returns></returns>
    Task<ReadOnlyMemory<byte>> LoadAsync(Guid key);
}

