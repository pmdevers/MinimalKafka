using RocksDbSharp;
using System.Collections.Concurrent;

namespace MinimalKafka.Stream.Storage.RocksDB;
internal sealed class RocksDBStreamStoreFactory : IDisposable, IKafkaStoreFactory
{
    private readonly RocksDb _db;
    private readonly ConcurrentDictionary<KafkaConsumerKey, ColumnFamilyHandle> _columnFamilies = new();

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
            _columnFamilies[CreateKey(existingFamilies[i])] = _db.GetColumnFamily(existingFamilies[i]);
        }
    }

    public IServiceProvider ServiceProvider { get; }

    private static KafkaConsumerKey CreateKey(string columnFamily)
    {
        var label = columnFamily.Split("-");
        if(label.Length == 1 ) 
        {
            return KafkaConsumerKey.Random(label[0]);
        }
        return new(label[0], label[1], label[2]);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    public IKafkaStore GetStore(KafkaConsumerKey consumerKey)
    {
        if (!_columnFamilies.TryGetValue(consumerKey, out var cfHandle))
        {
            // Create column family if it does not exist
            cfHandle = _db.CreateColumnFamily(new ColumnFamilyOptions(), 
                $"{consumerKey.TopicName}-{consumerKey.GroupId}-{consumerKey.ClientId}");
            _columnFamilies[consumerKey] = cfHandle;
        }

        return new RocksDBStreamStore(ServiceProvider, _db, cfHandle);
    }
}