namespace MinimalKafka.Stream;

public interface IStreamStore<TKey, TValue>
{
    TValue AddOrUpdate(TKey key, Func<TKey, TValue> create, 
        Func<TKey, TValue, TValue> update);
}

