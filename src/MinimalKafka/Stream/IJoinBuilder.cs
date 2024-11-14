using MinimalKafka.Builders;

namespace MinimalKafka.Stream;

public interface IJoinBuilder<K1, V1, K2, V2>
{
    IIntoBuilder<(V1, V2)> On(Func<V1, V2, bool> on);
    IIntoBuilder<TKey, (V1?, V2?)> On<TKey>(Func<K1, V1, TKey> leftKey, Func<K2, V2, TKey> rightKey);
}

public static class IJoinBuilderExtensions
{
    public static IIntoBuilder<TKey, (V1?, V2?)> OnKey<TKey, V1, V2>(this IJoinBuilder<TKey, V1, TKey, V2> builder)
        => builder.On((k, _) => k, (k, _) => k);
}
