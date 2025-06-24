using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream;

namespace MinimalKafka.RockDB.Tests;

public class UnitTest1
{
    public UnitTest1()
    {
        RocksDBHelper.ResetDatabase();
    }


    [Fact]
    public async Task Test1()
    {
        var services = new ServiceCollection();

        services.AddMinimalKafka(builder =>
        {
            builder.UseRocksDB(RocksDBHelper.DataPath);
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
    public static string DataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RocksDB");

    public static void ResetDatabase()
    {
        if (Directory.Exists(DataPath))
        {
            Directory.Delete(DataPath, true); // Deletes all database files
        }
    }
}