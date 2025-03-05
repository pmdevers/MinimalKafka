using RocksDbSharp;

namespace MinimalKafka.Stream.Storage.RocksDB;

public class RocksDBStreamStore<T1, T2> : IStreamStore<T1, T2>
{
    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _columnFamily;
    private readonly IByteSerializer _serializer;

    public RocksDBStreamStore(RocksDBOptions options, IByteSerializer serializer)
    {
        _db = RocksDb.Open(options.DBOptions, options.Path, options.ColumnFamilies);           
        _columnFamily = _db.GetDefaultColumnFamily();
        _serializer = serializer;
    }


    public ValueTask<T2> AddOrUpdate(T1 key, Func<T1, T2> create, Func<T1, T2, T2> update)
    {
        var keyBytes = _serializer.Serialize(key);
        var existingValueBytes = _db.Get(keyBytes, _columnFamily);

        T2 newValue;
        if (existingValueBytes == null)
        {
            newValue = create(key);
        }
        else
        {
            var existingValue = _serializer.Deserialize<T2>(existingValueBytes);
            newValue = update(key, existingValue);
        }

        _db.Put(keyBytes, _serializer.Serialize(newValue), _columnFamily);

        return ValueTask.FromResult(newValue);
    }

    public async IAsyncEnumerable<T2> FindAsync(Func<T2, bool> predicate)
    {
        using var iterator = _db.NewIterator(_columnFamily);
        for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
        {
            var value = _serializer.Deserialize<T2>(iterator.Value());
            if (predicate(value))
            {
                yield return value;
                await Task.Yield(); // Ensure the method is async
            }
        }
    }

    public ValueTask<T2?> FindByIdAsync(T1 key)
    {
        var keyBytes = _serializer.Serialize(key);
        var valueBytes = _db.Get(keyBytes, _columnFamily);

        if (valueBytes == null)
            return ValueTask.FromResult<T2?>(default);

        var value = _serializer.Deserialize<T2>(valueBytes);
        return ValueTask.FromResult<T2?>(value);
    }
}
