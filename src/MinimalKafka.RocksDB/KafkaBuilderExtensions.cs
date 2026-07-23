using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream.Storage.RocksDB;

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
    public static TBuilder WithRocksDB<TBuilder>(this TBuilder builder, Action<RocksDBOptions>? options = null)
        where TBuilder : IKafkaConfigBuilder
    {
        builder.Services.Configure(options ?? (_ => { }));
        builder.WithStoreFactory<TBuilder, RocksDBStreamStoreFactory>();
        return builder;
    }
}
