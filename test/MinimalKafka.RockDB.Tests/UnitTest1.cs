using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Storage.RocksDB;

namespace MinimalKafka.RockDB.Tests;

public class UnitTest1
{
    public UnitTest1()
    {
        RocksDBHelper.ResetDatabase("c:\\SourceCode\\rocksdb");
    }


    [Fact]
    public async Task Test1()
    {
        var services = new ServiceCollection();

        services.AddMinimalKafka(builder =>
        {
            builder.UseRocksDB();
        });

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IStreamStoreFactory>();

        var streamStore = factory.GetStreamStore<string, string>();

        var result = await streamStore.AddOrUpdate("key", _ => "value", (_, _) => "value2");

        var value = await streamStore.FindByIdAsync("key");

        Assert.Equal("value", value);
    }
}


public static class RocksDBHelper
{
    public static void ResetDatabase(string dbPath)
    {
        if (Directory.Exists(dbPath))
        {
            Directory.Delete(dbPath, true); // Deletes all database files
        }
    }
}