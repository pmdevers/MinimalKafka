using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Extension;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Storage.RocksDB;
using RocksDbSharp;

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
    /// <param name="path"></param>
    /// <returns></returns>
    public static IAddKafkaBuilder UseRocksDB(this IAddKafkaBuilder builder, string? path = null)
    {
        var dataPath = path ?? 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "RockDB");

        Directory.CreateDirectory(dataPath);
        
        builder.WithStreamStoreFactory(new RocksDBStreamStoreFactory(dataPath));
        return builder;
    }
}
