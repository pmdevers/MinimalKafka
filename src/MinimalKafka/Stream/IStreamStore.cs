namespace MinimalKafka.Stream;

public interface IStreamStore<TKey, TValue>
{
    ValueTask<TValue> AddOrUpdate(TKey key, Func<TKey, TValue> create, 
        Func<TKey, TValue, TValue> update);

    ValueTask<TValue?> FindByIdAsync(TKey key);

    IAsyncEnumerable<TValue> FindAsync(Func<TValue, bool> predicate);
}

