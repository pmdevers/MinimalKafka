using Microsoft.Extensions.Options;
using RocksDbSharp;
using System.Collections.Concurrent;

namespace MinimalKafka.Stream.Storage.RocksDB;

internal sealed class RocksDBStreamStoreFactory : IDisposable, IKafkaStoreFactory
{
    private readonly RocksDb _db;
    private readonly ConcurrentDictionary<string, ColumnFamilyHandle> _columnFamilies = new();

    public RocksDBStreamStoreFactory(IServiceProvider serviceProvider, IOptions<RocksDBOptions> config)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Value.DataPath);

        ServiceProvider = serviceProvider;
        Config = config;

        var options = new DbOptions()
            .SetCreateIfMissing(true)
            .SetCreateMissingColumnFamilies(true);

        Directory.CreateDirectory(Config.Value.DataPath);

        // Load existing column families
        // Get existing column families or default if database is new
        string[] existingFamilies;
        try
        {
            existingFamilies = [.. RocksDb.ListColumnFamilies(options, Config.Value.DataPath)];
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

        _db = RocksDb.Open(options, Config.Value.DataPath, cfDescriptors);

        // Store all handles
        for (int i = 0; i < existingFamilies.Length; i++)
        {
            _columnFamilies[existingFamilies[i]] = _db.GetColumnFamily(existingFamilies[i]);
        }
    }

    public IServiceProvider ServiceProvider { get; }
    public IOptions<RocksDBOptions> Config { get; }

    public void Dispose()
    {
        _db?.Dispose();
    }

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

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