using RocksDbSharp;

namespace MinimalKafka.Stream.Storage.RocksDB;

/// <summary>
/// Options for configuring RocksDB.
/// </summary>
public class RocksDBOptions
{
    /// <summary>
    /// The file system path where the RocksDB database will be stored.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Options for configuring the RocksDB database.
    /// </summary>
    public DbOptions DBOptions { get; set; } = new DbOptions()
        .SetCreateIfMissing(true)
        .SetCreateMissingColumnFamilies(true);

    /// <summary>
    /// Collection of column families and their options for the RocksDB database.
    /// </summary>
    public ColumnFamilies ColumnFamilies { get; set; } = new ColumnFamilies(new ColumnFamilyOptions());
}
