namespace MinimalKafka.Stream.Internals;

internal sealed class JoinByKeyIntoBuilder<TKey, K1, V1, K2, V2>(
        IKafkaBuilder builder,
        string leftTopic,
        string rightTopic,
        bool innerJoin,
        Func<K1, V1, TKey> leftKey,
        Func<K2, V2, TKey> rightKey) : IIntoBuilder<TKey, (V1?, V2?)>
    where TKey : notnull
    where K1 : notnull
    where K2 : notnull
{
    private Func<KafkaContext, TKey, (V1?, V2?), Task> _into = (_, _, _) => Task.CompletedTask;

    public IKafkaConventionBuilder Into(Func<KafkaContext, TKey, (V1?, V2?), Task> handler)
    {
        _into = handler;

        var l = builder.MapTopic(leftTopic, ExecuteLeftAsync);
        var r = builder.MapTopic(rightTopic, ExecuteRightAsync);

        return new JoinConventionBuilder(l, r);
    }

    public async Task Handle(KafkaContext context, TKey key, (V1?, V2?) value)
    {
        if (value.Item1 is null || (innerJoin && value.Item2 is null))
        {
            return;
        }
        await _into(context, key, value);
    }


    public async Task ExecuteLeftAsync(KafkaContext context, K1 key, V1 value)
    {
        var rightStore = context.GetTopicStore(rightTopic);

        var lkey = leftKey(key, value);
        var rvalue = await rightStore.FindByKey<TKey, V2>(lkey);

        await Handle(context, lkey, (value, rvalue));
    }

    public async Task ExecuteRightAsync(KafkaContext context, K2 key, V2 value)
    {
        var leftStore = context.GetTopicStore(leftTopic);
                
        var rkey = rightKey(key, value);
        var lvalue = await leftStore.FindByKey<TKey, V1>(rkey);

        await Handle(context, rkey, (lvalue, value));
    }
}