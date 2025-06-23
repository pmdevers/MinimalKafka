using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Extension;
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
    /// <param name="options"></param>
    /// <summary>
    /// Configures the Kafka builder to use RocksDB as the stream storage backend.
    /// </summary>
    /// <param name="options">A delegate to configure the <see cref="RocksDBOptions"/> for the RocksDB instance.</param>
    /// <returns>The configured <see cref="IAddKafkaBuilder"/> instance for fluent chaining.</returns>
    public static IAddKafkaBuilder UseRocksDB(this IAddKafkaBuilder builder, Action<RocksDBOptions> options)
    {
        var o = new RocksDBOptions();

        o.DBOptions.SetCreateIfMissing(true);
        o.DBOptions.SetCreateMissingColumnFamilies(true);
        options(o);

        Directory.CreateDirectory(o.Path);

        var cfNames = RocksDb.ListColumnFamilies(o.DBOptions, o.Path);
        
        foreach(var f in cfNames) 
        {
            o.ColumnFamilies.Add(f, new ColumnFamilyOptions());
        }
        
        builder.Services.AddSingleton(RocksDb.Open(o.DBOptions, o.Path, o.ColumnFamilies));
        builder.Services.AddSingleton(o);
        builder.Services.AddSingleton<IByteSerializer, ByteSerializer>();
        builder.Services.AddSingleton(typeof(RocksDBStreamStore<,>));

        builder.WithStreamStore(typeof(RocksDBStreamStore<,>));
        return builder;
    }
}
