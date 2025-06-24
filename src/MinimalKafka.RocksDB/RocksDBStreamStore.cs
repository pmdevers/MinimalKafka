using RocksDbSharp;

namespace MinimalKafka.Stream.Storage.RocksDB;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
internal class RocksDBStreamStore<TKey, TValue>(RocksDb db, ColumnFamilyHandle cfHandle, IByteSerializer serializer) : IStreamStore<TKey, TValue>
{
    public async ValueTask<TValue> AddOrUpdate(TKey key, Func<TKey, TValue> create, Func<TKey, TValue, TValue> update)
    {
        var keyBytes = serializer.Serialize(key);
        var existingBytes = db.Get(keyBytes, cfHandle);
        TValue value;

        if (existingBytes == null)
        {
            value = create(key);
        }
        else
        {
            var existingValue = serializer.Deserialize<TValue>(existingBytes);
            value = update(key, existingValue);
        }

        var valueBytes = serializer.Serialize(value);
        db.Put(keyBytes, valueBytes, cfHandle);

        return value;
    }

    public async ValueTask<TValue?> FindByIdAsync(TKey key)
    {
        var keyBytes = serializer.Serialize(key);
        var valueBytes = db.Get(keyBytes, cfHandle);
        if (valueBytes == null)
            return default;

        return serializer.Deserialize<TValue>(valueBytes);
    }

    public async IAsyncEnumerable<TValue> FindAsync(Func<TValue, bool> predicate)
    {
        using var iterator = db.NewIterator(cfHandle);
        for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
        {
            var value = serializer.Deserialize<TValue>(iterator.Value());
            if (predicate(value))
                yield return value;
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously