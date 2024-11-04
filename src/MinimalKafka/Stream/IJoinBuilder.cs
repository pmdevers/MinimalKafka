namespace MinimalKafka.Stream;

public interface IJoinBuilder<K1, V1, K2, V2>
{
    IIntoBuilder<TKey, Tuple<V1?, V2?>> On<TKey>(
        Func<K1, V1, TKey> leftKey, 
        Func<K2, V2, TKey> rightKey);
}

public static class JoinBuilderExtensions
{
    public static IIntoBuilder<TKey, Tuple<V1?, V2?>> OnKey<TKey, V1, V2>(this IJoinBuilder<TKey, V1, TKey, V2> builder)
        => builder.On((k1, _) => k1, (k2, _) => k2);
}

