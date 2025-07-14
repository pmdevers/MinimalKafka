using System.Text.Json;

namespace MinimalKafka.Stream.Storage.RocksDB;

/// <summary>
/// Options for configuring RocksDB stream storage.
/// </summary>
public class RocksDBOptions
{
    /// <summary>
    /// Gets or sets the file system path where application data is stored.
    /// </summary>
    public string DataPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RocksDB");
}
