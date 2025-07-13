using RocksDbSharp;
using System.Threading.Tasks;

namespace MinimalKafka.Stream.Storage.RocksDB;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
internal class RocksDBStreamStore(IServiceProvider serviceProvider, RocksDb db, ColumnFamilyHandle cfHandle) : IKafkaStore
{
    public IServiceProvider ServiceProvider => serviceProvider;

    public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        var lkey = new byte[key.Length];
        var lvalue = new byte[value.Length];

        key.CopyTo(lkey);
        value.CopyTo(lvalue);
        
        db.Put(lkey, lvalue, cfHandle);
        return ValueTask.FromResult(lvalue);
    }

    public ValueTask<byte[]> FindByIdAsync(byte[] key)
    {
        var result = db.Get(key, cfHandle);
        return ValueTask.FromResult(result);
    }

    public async IAsyncEnumerable<byte[]> GetItems()
    {
        using var iterator = db.NewIterator(cfHandle);
        for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
        {
            yield return iterator.Value();
        }
    }
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously