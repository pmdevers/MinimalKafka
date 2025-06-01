using RocksDbSharp;

namespace MinimalKafka.Stream.Storage.RocksDB;

public class RocksDBOptions
{
    public string Path { get; set; } = string.Empty;
    public DbOptions DBOptions { get; set; } = new DbOptions()
        .SetCreateIfMissing(true)
        .SetCreateMissingColumnFamilies(true);

    public ColumnFamilies ColumnFamilies { get; set; } = new ColumnFamilies(new ColumnFamilyOptions());
}
