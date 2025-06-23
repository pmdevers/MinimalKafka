using RocksDbSharp;

namespace MinimalKafka.Stream.Storage.RocksDB;


internal class RocksDBStreamStore<T1, T2> : IStreamStore<T1, T2>
{
    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _columnFamily;
    private readonly IByteSerializer _serializer;



    /// <summary>
    /// Initializes a new instance of the <see cref="RocksDBStreamStore{T1, T2}"/> class using the specified RocksDB instance and serializer.
    /// </summary>
    /// <param name="db">The RocksDB database instance to use for storage.</param>
    /// <param name="serializer">The serializer for serializing and deserializing keys and values.</param>
    public RocksDBStreamStore(RocksDb db, IByteSerializer serializer)
    {
        _db = db;
        if (!_db.TryGetColumnFamily(typeof(T2).ToString(), out _columnFamily))
        {
            _columnFamily = _db.CreateColumnFamily(new ColumnFamilyOptions(), typeof(T2).ToString());
        }
        _serializer = serializer;
    }


    /// <summary>
    /// Adds a new value or updates an existing value for the specified key in the RocksDB store.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="create">A function to generate a new value if the key does not exist.</param>
    /// <param name="update">A function to update the existing value if the key is found.</param>
    /// <returns>The newly added or updated value.</returns>
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

    /// <summary>
    /// Asynchronously enumerates all values in the store that satisfy the specified predicate.
    /// </summary>
    /// <param name="predicate">A function to filter values of type T2.</param>
    /// <returns>An asynchronous stream of values matching the predicate.</returns>
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

    /// <summary>
    /// Asynchronously retrieves a value by its key from the RocksDB column family.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The value associated with the specified key, or null if not found.</returns>
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
