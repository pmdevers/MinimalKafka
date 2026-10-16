namespace MinimalKafka.Stream;

/// <summary>
/// Stores stream records locally for joins and projections.
/// </summary>
public interface IKafkaStore
{
    /// <summary>The application service provider associated with this store.</summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>The physical Kafka topic name represented by this store.</summary>
    string TopicName { get; }

    /// <summary>Adds or replaces a value for the specified serialized key.</summary>
    ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value);

    /// <summary>Finds a stored value by its serialized key.</summary>
    ValueTask<byte[]?> FindByKeyAsync(ReadOnlySpan<byte> key);

    /// <summary>Returns all stored values for the topic.</summary>
    IAsyncEnumerable<byte[]> GetItems();
}

/// <summary>
/// Creates per-topic stream stores.
/// </summary>
public interface IKafkaStoreFactory : IDisposable
{
    /// <summary>The application service provider associated with this factory.</summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>Gets a stream store for the specified physical Kafka topic.</summary>
    IKafkaStore GetStore(string topicName);
}
