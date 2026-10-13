using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Storage.RocksDB;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace MinimalKafka;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for configuring Kafka with RocksDB as the stream store.
/// </summary>
public static class KafkaBuilderExtensions
{
    extension(IMinimalKafkaBuilder builder)
    {
        /// <summary>
        /// Configures the <see cref="IMinimalKafkaBuilder"/> to use RocksDB as the stream store.
        /// </summary>
        public IMinimalKafkaBuilder WithRocksDB(Action<RocksDBOptions>? options = null)
        {
            builder.Services.AddOptions<RocksDBOptions>();
            builder.Services.Configure(options ?? (_ => { }));
            builder.Services.Replace(ServiceDescriptor.Singleton<IKafkaStoreFactory, RocksDBStreamStoreFactory>());
            return builder;
        }
    }
}
