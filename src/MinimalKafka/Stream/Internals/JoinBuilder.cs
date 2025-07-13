namespace MinimalKafka.Stream.Internals;

internal sealed class JoinBuilder<K1, V1, K2, V2>(IKafkaBuilder builder, string leftTopic, string rightTopic, bool innerJoin)
    : IJoinBuilder<K1, V1, K2, V2>
    where K1 : notnull
    where K2 : notnull
{
    public IIntoBuilder<(V1, V2)> On(Func<V1, V2, bool> on)
        => new JoinIntoBuilder<K1, V1, K2, V2>(builder, leftTopic, rightTopic, on);

    public IIntoBuilder<TKey, (V1?, V2?)> On<TKey>(Func<K1, V1, TKey> leftKey, Func<K2, V2, TKey> rightKey)
        where TKey : notnull
        => new JoinByKeyIntoBuilder<TKey, K1, V1, K2, V2>(builder, leftTopic, rightTopic, innerJoin, leftKey, rightKey);
}
