using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream.Storage.RocksDB;

namespace MinimalKafka;

public static class KafkaBuilderExtensions
{
    public static IAddKafkaBuilder UseRocksDB(this IAddKafkaBuilder builder, Action<RocksDBOptions> options)
    {
        var rocksDBOptions = new RocksDBOptions();
        options(rocksDBOptions);

        builder.Services.AddSingleton(rocksDBOptions);
        builder.Services.AddSingleton<IByteSerializer, ByteSerializer>();
        builder.Services.AddSingleton(typeof(RocksDBStreamStore<,>));

        builder.WithStreamStore(typeof(RocksDBStreamStore<,>));
        return builder;
    }
}
