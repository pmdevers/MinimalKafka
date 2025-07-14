using Microsoft.Extensions.DependencyInjection;
using System.Text;

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
            builder.UseRocksDB(o =>
            {
                o.DataPath = RocksDBHelper.DataPath;
            });
        });
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IKafkaStoreFactory>();
        var streamStore = factory.GetStore(KafkaConsumerKey.Random("test"));
        var key = Encoding.UTF8.GetBytes("key");
        var value = Encoding.UTF8.GetBytes("value");

        // Test adding new key
        await streamStore.AddOrUpdate(key, value);

        var val = await streamStore.FindByIdAsync(key);

        Assert.Equal(val,  value);
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