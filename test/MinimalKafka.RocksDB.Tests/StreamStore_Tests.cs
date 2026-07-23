using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream.Storage.RocksDB;
using System.Text;

namespace MinimalKafka.RocksDB.Tests;

public class StreamStore_Tests
{
    [Fact]
    public async Task WithRocksDB_Should_Register_RocksdbFactory()
    {
        RocksDBHelper.ResetDatabase();

        var services = new ServiceCollection();
        services.AddMinimalKafka(builder =>
        {
            builder.WithRocksDB(o =>
            {
                o.DataPath = RocksDBHelper.DataPath;
            });
        });
        var provider = services.BuildServiceProvider();
        using var factory = provider.GetRequiredService<IKafkaStoreFactory>();
        Assert.NotNull(factory);
        Assert.IsType<RocksDBStreamStoreFactory>(factory);

    }


    [Fact]
    public async Task AddOrUpdate_WithNewKey_ShouldAddValue()
    {
        RocksDBHelper.ResetDatabase();

        var services = new ServiceCollection();
        services.AddMinimalKafka(builder =>
        {
            builder.WithRocksDB(o =>
            {
                o.DataPath = RocksDBHelper.DataPath;
            });
        });
        var provider = services.BuildServiceProvider();
        using var factory = provider.GetRequiredService<IKafkaStoreFactory>();
        var streamStore = factory.GetStore("test");
        var key = Encoding.UTF8.GetBytes("key");
        var value = Encoding.UTF8.GetBytes("value");

        // Test adding new key
        await streamStore.AddOrUpdate(key, value);

        var val = await streamStore.FindByKeyAsync(key);

        Assert.Equal(val, value);
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