using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream;

namespace MinimalKafka.RocksDB.Tests;

public class StreamStore_Tests
{
    public StreamStore_Tests()
    {
        RocksDBHelper.ResetDatabase();
    }


    [Fact]
    public async Task AddOrUpdate_WithNewKey_ShouldAddValue()
    {
        var services = new ServiceCollection();
        services.AddMinimalKafka(builder =>
        {
            builder.UseRocksDB(RocksDBHelper.DataPath);
        });
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IStreamStoreFactory>();
        var streamStore = factory.GetStreamStore<string, string>();
        // Test adding new key
        await streamStore.AddOrUpdate("key", _ => "value", (_, _) => "value2");
        var value = await streamStore.FindByIdAsync("key");
        Assert.Equal("value", value);
        // Test updating existing key
        await streamStore.AddOrUpdate("key", _ => "value", (_, _) => "value2");
        var updatedValue = await streamStore.FindByIdAsync("key");
        Assert.Equal("value2", updatedValue);
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