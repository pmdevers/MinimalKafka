using Confluent.Kafka;
using RocksDbSharp;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MinimalKafka.Stream.Storage.RocksDB;
internal sealed class RocksDBStreamStoreFactory : IDisposable, IStreamStoreFactory
{
    private readonly RocksDb _db;
    private readonly ConcurrentDictionary<string, ColumnFamilyHandle> _columnFamilies = new();
    private readonly RocksDBOptions _config;

    public RocksDBStreamStoreFactory(RocksDBOptions config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config;

        var options = new DbOptions()
            .SetCreateIfMissing(true)
            .SetCreateMissingColumnFamilies(true);

        // Load existing column families
        // Get existing column families or default if database is new
        string[] existingFamilies;
        try
        {
            existingFamilies = [.. RocksDb.ListColumnFamilies(options, _config.DataPath)];
        }
        catch
        {
            existingFamilies = ["default"];
        }

        var cfDescriptors = new ColumnFamilies();

        foreach (var name in existingFamilies)
        {
            cfDescriptors.Add(name, new ColumnFamilyOptions());
        }
        
        db = RocksDb.Open(options, _config.DataPath, cfDescriptors);

        // Store all handles
        for (int i = 0; i < existingFamilies.Length; i++)
        {
            _columnFamilies[existingFamilies[i]] = _db.GetColumnFamily(existingFamilies[i]);
        }
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    public IStreamStore<TKey, TValue> GetStreamStore<TKey, TValue>()
        where TKey : notnull
    {
        var storeName = typeof(TValue).Name;
        if (!_columnFamilies.TryGetValue(storeName, out var cfHandle))
        {
            // Create column family if it does not exist
            cfHandle = _db.CreateColumnFamily(new ColumnFamilyOptions(), storeName);
            _columnFamilies[storeName] = cfHandle;
        }

        return new RocksDBStreamStore<TKey, TValue>(_db, cfHandle, _config.Serializer);
    }
}