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
            "RockDB");

    /// <summary>
    /// Gets or sets the serializer used for converting objects to and from byte arrays.
    /// </summary>
    /// <remarks>The serializer can be customized to use different serialization formats or settings,
    /// depending on the implementation of <see cref="IByteSerializer"/>.</remarks>
    public IByteSerializer Serializer { get; set; } = new ByteSerializer(JsonSerializerOptions.Default);
}
