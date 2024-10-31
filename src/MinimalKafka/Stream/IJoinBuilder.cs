namespace MinimalKafka.Stream;

public interface IJoinBuilder<K1, V1, K2, V2>
{
    IIntoBuilder<TKey, Tuple<V1?, V2?>> On<TKey>(
        Func<K1, V1, TKey> leftKey, 
        Func<K2, V2, TKey> rightKey);
}

