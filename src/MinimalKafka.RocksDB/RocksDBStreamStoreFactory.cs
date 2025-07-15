using RocksDbSharp;
using System.Collections.Concurrent;

namespace MinimalKafka.Stream.Storage.RocksDB;
internal sealed class RocksDBStreamStoreFactory : IDisposable, IKafkaStoreFactory
{
    private readonly RocksDb _db;
    private readonly ConcurrentDictionary<string, ColumnFamilyHandle> _columnFamilies = new();

    public RocksDBStreamStoreFactory(IServiceProvider serviceProvider, RocksDBOptions config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ServiceProvider = serviceProvider;

        var options = new DbOptions()
            .SetCreateIfMissing(true)
            .SetCreateMissingColumnFamilies(true);

        // Load existing column families
        // Get existing column families or default if database is new
        string[] existingFamilies;
        try
        {
            existingFamilies = [.. RocksDb.ListColumnFamilies(options, config.DataPath)];
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
        
        _db = RocksDb.Open(options, config.DataPath, cfDescriptors);

        // Store all handles
        for (int i = 0; i < existingFamilies.Length; i++)
        {
            _columnFamilies[existingFamilies[i]] = _db.GetColumnFamily(existingFamilies[i]);
        }
    }

    public IServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
        _db?.Dispose();
    }

    private readonly object _lock = new();
    public IKafkaStore GetStore(string topicName)
    {

        lock (_lock)
        {
            var cfHandle = _columnFamilies.GetOrAdd(topicName, key =>
            {
                // Only create if truly absent
                return _db.CreateColumnFamily(new ColumnFamilyOptions(), key);
            });
               
            return new RocksDBStreamStore(ServiceProvider, _db, cfHandle);
        }
    }
}