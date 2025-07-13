using RocksDbSharp;
using System.Threading.Tasks;

namespace MinimalKafka.Stream.Storage.RocksDB;


internal class RocksDBStreamStore(IServiceProvider serviceProvider, RocksDb db, ColumnFamilyHandle cfHandle) : IKafkaStore
{
    public IServiceProvider ServiceProvider => serviceProvider;

    public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        db.Put(key.ToArray(), value.ToArray(), cfHandle);
        return ValueTask.FromResult(value.ToArray());
    }

    public ValueTask<byte[]?> FindByIdAsync(ReadOnlySpan<byte> key)
    {
        var result = db.Get(key, cfHandle);
        return ValueTask.FromResult<byte[]?>(result);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<byte[]> GetItems()
    {
        using var iterator = db.NewIterator(cfHandle);
        for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
        {
            yield return iterator.Value();
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
