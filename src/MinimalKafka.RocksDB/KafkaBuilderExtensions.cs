using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream.Storage.RocksDB;
using RocksDbSharp;

namespace MinimalKafka;

public static class KafkaBuilderExtensions
{
    public static IAddKafkaBuilder UseRocksDB(this IAddKafkaBuilder builder, Action<RocksDBOptions> options)
    {
        var o = new RocksDBOptions();
        options(o);

        var fam = RocksDb.ListColumnFamilies(o.DBOptions, o.Path);
        foreach (var f in fam)
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
