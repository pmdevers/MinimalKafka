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
    /// Configures the <see cref="IKafkaConfigBuilder"/> to use RocksDB as the stream store.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IKafkaConfigBuilder UseRocksDB(this IKafkaConfigBuilder builder, Action<RocksDBOptions>? options = null)
    {
        var config = new RocksDBOptions();
        options?.Invoke(config);

        Directory.CreateDirectory(config.DataPath);

        builder.UseStoreFactory(s => new RocksDBStreamStoreFactory(s,config));
        return builder;
    }
}
