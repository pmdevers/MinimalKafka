using MinimalKafka.Builders;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

internal class JoinBuilder<K1, V1, K2, V2> : IJoinBuilder<K1, V1, K2, V2>
{
    private readonly ISourceBlock<Tuple<KafkaContext, K1, V1>> _left;
    private readonly BufferBlock<Tuple<KafkaContext, K2, V2>> _right;

    public JoinBuilder(IKafkaBuilder builder,
        ISourceBlock<Tuple<KafkaContext, K1, V1>> left, string topic)
    {
        _left = left;
        _right = new BufferBlock<Tuple<KafkaContext, K2, V2>>();
        builder.MapTopic(topic, async (KafkaContext context, K2 key, V2 value) =>
        {
            await _right.SendAsync(Tuple.Create(context, key, value));
            await _right.Completion;
        });
    }
    public IIntoBuilder<TKey, Tuple<V1?, V2?>> On<TKey>(IStreamStore<TKey, Tuple<V1?, V2?>> store, Func<K1, V1, TKey> leftKey, Func<K2, V2, TKey> rightKey)
    {
        var transformLeftKey = new TransformBlock<Tuple<KafkaContext, K1, V1>,
            Tuple<KafkaContext, TKey, V1>>(GetKey(leftKey));

        var transformRightKey = new TransformBlock<Tuple<KafkaContext, K2, V2>,
            Tuple<KafkaContext, TKey, V2>>(GetKey(rightKey));

       
        var storeLeft = new TransformBlock<Tuple<KafkaContext, TKey, V1>, Tuple<KafkaContext, TKey, Tuple<V1?, V2?>>>(GetFromStorageV1(store));
        var storeRight = new TransformBlock<Tuple<KafkaContext, TKey, V2>, Tuple<KafkaContext, TKey, Tuple<V1?, V2?>>>(GetFromStorageV2(store));

        _left.LinkTo(transformLeftKey);
        _right.LinkTo(transformRightKey);

        transformLeftKey.LinkTo(storeLeft);
        transformRightKey.LinkTo(storeRight);

        return new IntoBuilder<TKey, Tuple<V1?, V2?>>(storeLeft, storeRight);
    }

    private static Func<
        Tuple<KafkaContext, TKey, V2>, 
        Tuple<KafkaContext, TKey, Tuple<V1?, V2?>>
        > GetFromStorageV2<TKey>(IStreamStore<TKey, Tuple<V1?, V2?>> store)
    {
        return data =>
        {
            var result = store.AddOrUpdate(data.Item2,
                    (k) => Tuple.Create<V1?, V2?>(default, data.Item3),
                    (k, v) => Tuple.Create<V1?, V2?>(v.Item1, data.Item3));
            
            return Tuple.Create(data.Item1, data.Item2, result);
        };
    }

    private static Func<
        Tuple<KafkaContext, TKey, V1>, 
        Tuple<KafkaContext, TKey, Tuple<V1?, V2?>>
        > GetFromStorageV1<TKey>(IStreamStore<TKey, Tuple<V1?, V2?>> store)
    {
        return data =>
        {
            var result = store.AddOrUpdate(data.Item2,
                (k) => Tuple.Create<V1?, V2?>(data.Item3, default),
                (k, v) => Tuple.Create<V1?, V2?>(data.Item3, v.Item2));

            return Tuple.Create(data.Item1, data.Item2, result);
        };
    }

    private static Func<
        Tuple<KafkaContext, TKey, TValue>, 
        Tuple<KafkaContext, TReturn, TValue>
        > GetKey<TKey, TValue, TReturn>(Func<TKey, TValue, TReturn> keySelector)
    {
        return (data) =>
        {
            var key = keySelector(data.Item2, data.Item3);
            return Tuple.Create(data.Item1, key, data.Item3);
        };
    }
}

