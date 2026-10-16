using RocksDbSharp;

namespace MinimalKafka.Stream.Storage.RocksDB;

internal sealed class RocksDBStreamStore(
    IServiceProvider serviceProvider,
    string topicName,
    RocksDb db,
    ColumnFamilyHandle cfHandle) : IKafkaStore
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public string TopicName { get; } = topicName;

    public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        var serializedValue = value.ToArray();
        db.Put(key.ToArray(), serializedValue, cfHandle);
        return ValueTask.FromResult(serializedValue);
    }

    public ValueTask<byte[]?> FindByKeyAsync(ReadOnlySpan<byte> key)
    {
        var result = db.Get(key, cfHandle);
        return ValueTask.FromResult<byte[]?>(result);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<byte[]> GetItems()
    {
        using var iterator = db.NewIterator(cfHandle);
        for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
        {
            yield return iterator.Value();
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
