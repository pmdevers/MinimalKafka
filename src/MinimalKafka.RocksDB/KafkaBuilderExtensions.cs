using MinimalKafka.Builders;
using MinimalKafka.Extension;
using MinimalKafka.Stream.Storage.RocksDB;
using System.Text.Json;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace MinimalKafka;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for configuring Kafka with RocksDB as the stream store.
/// </summary>
public static class KafkaBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="IAddKafkaBuilder"/> to use RocksDB as the stream store.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IAddKafkaBuilder UseRocksDB(this IAddKafkaBuilder builder, Action<RocksDBOptions>? options = null)
    {
        var config = new RocksDBOptions();
        options?.Invoke(config);

        Directory.CreateDirectory(config.DataPath);
        
        builder.WithStreamStoreFactory(new RocksDBStreamStoreFactory(config));
        return builder;
    }

    /// <summary>
    /// Configures the specified <see cref="RocksDBOptions"/> to use a JSON-based serializer.
    /// </summary>
    /// <remarks>This method sets the <see cref="RocksDBOptions.Serializer"/> property to a serializer that
    /// uses JSON serialization. Use the <paramref name="options"/> parameter to customize the behavior of the JSON
    /// serializer, such as formatting or type handling.</remarks>
    /// <param name="rockDBOptions">The <see cref="RocksDBOptions"/> instance to configure.</param>
    /// <param name="options">An optional delegate to configure the <see cref="JsonSerializerOptions"/> used by the serializer. If not
    /// provided, default options are used.</param>
    /// <returns>The configured <see cref="RocksDBOptions"/> instance.</returns>
    public static RocksDBOptions UseJsonSerializer(this RocksDBOptions rockDBOptions, Action<JsonSerializerOptions>? options = null)
    {
        var config = new JsonSerializerOptions();
        options?.Invoke(config);

        rockDBOptions.Serializer = new ByteSerializer(config);

        return rockDBOptions;
    }
}
