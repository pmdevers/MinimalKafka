using Microsoft.Extensions.Options;
using RocksDbSharp;
using System.Collections.Concurrent;

namespace MinimalKafka.Stream.Storage.RocksDB;

/// <summary>
/// A factory for creating instances of <see cref="RocksDBStreamStore"/> for different Kafka topics.
/// </summary>
public sealed class RocksDBStreamStoreFactory : IKafkaStoreFactory
{
    private readonly RocksDb _db;
    private readonly ConcurrentDictionary<string, ColumnFamilyHandle> _columnFamilies = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RocksDBStreamStoreFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="config">The RocksDB configuration options.</param>
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

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the RocksDB configuration options.
    /// </summary>
    public IOptions<RocksDBOptions> Config { get; }

    /// <summary>
    /// Disposes the RocksDB instance and releases any resources used by the factory.
    /// </summary>
    public void Dispose()
    {
        _db?.Dispose();
    }

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    /// <inheritdoc />
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