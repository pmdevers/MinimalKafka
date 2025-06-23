using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream.Storage.RocksDB;

namespace MinimalKafka.RockDB.Tests;

public class UnitTest1
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitTest1"/> class and resets the RocksDB database at the specified path.
    /// </summary>
    public UnitTest1()
    {
        RocksDBHelper.ResetDatabase("c:\\SourceCode\\rocksdb");
    }


    /// <summary>
    /// Tests adding and retrieving a key-value pair in a RocksDB-backed stream store using dependency injection.
    /// </summary>
    [Fact]
    public async Task Test1()
    {
        var services = new ServiceCollection();

        services.AddMinimalKafka(builder =>
        {
            builder.UseRocksDB(options =>
            {
                options.Path = "c:\\SourceCode\\rocksdb";
            });
        });

        var provider = services.BuildServiceProvider();

        var streamStore = provider.GetRequiredService<RocksDBStreamStore<string, string>>();

        var result = await streamStore.AddOrUpdate("key", _ => "value", (_, _) => "value2");

        var value = await streamStore.FindByIdAsync("key");

        Assert.Equal("value", value);
    }
}


public static class RocksDBHelper
{
    /// <summary>
    /// Deletes the RocksDB database directory at the specified path if it exists, removing all contents.
    /// </summary>
    /// <param name="dbPath">The file system path to the RocksDB database directory.</param>
    public static void ResetDatabase(string dbPath)
    {
        if (Directory.Exists(dbPath))
        {
            Directory.Delete(dbPath, true); // Deletes all database files
        }
    }
}