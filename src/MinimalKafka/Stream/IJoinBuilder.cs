using MinimalKafka.Builders;

namespace MinimalKafka.Stream;

public interface IJoinBuilder<V1, V2>
{
    IIntoBuilder<(V1, V2)> On(Func<V1, V2, bool> on);
}

public interface IJoinBuilder<K1, V1, K2, V2> : IJoinBuilder<V1, V2>
{
    IIntoBuilder<TKey, (V1?, V2?)> On<TKey>(Func<K1, V1, TKey> leftKey, Func<K2, V2, TKey> rightKey);
}

