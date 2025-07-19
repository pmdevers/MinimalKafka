namespace MinimalKafka.Stream.Internals;

internal class ToBranchBuilder<TKey, TValue>(
    IBranchBuilder<TKey, TValue> builder,
    Func<TKey, TValue, bool> selector)
    : IToBranchBuilder<TKey, TValue>
{
    public IBranchBuilder<TKey, TValue> To(string topicName) =>
        builder.Branch(selector, async (c, k, v) => await c.ProduceAsync(topicName, k, v));
}