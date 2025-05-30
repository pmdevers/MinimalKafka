using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;

namespace MinimalKafka.Stream.Internals;

internal class JoinByKeyIntoBuilder<TKey, K1, V1, K2, V2>(
        IKafkaBuilder builder,
        string leftTopic,
        string rightTopic,
        Func<K1, V1, TKey> leftKey,
        Func<K2, V2, TKey> rightKey) : IIntoBuilder<TKey, (V1?, V2?)>
{
    private readonly Func<KafkaContext, IStreamStore<TKey, V1>> _getLeftStore = 
        context => context.RequestServices.GetRequiredService<IStreamStore<TKey, V1>>();

    private readonly Func<KafkaContext, IStreamStore<TKey, V2>> _getRightStore = 
        context => context.RequestServices.GetRequiredService<IStreamStore<TKey, V2>>();

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
        if (value.Item1 is null || value.Item2 is null)
        {
            return;   
        }
        await _into(context, key, value);
    }


    public async Task ExecuteLeftAsync(KafkaContext context, K1 key, V1 value)
    {
        var leftStore = _getLeftStore(context);
        var rightStore = _getRightStore(context);

        var lkey = leftKey(key, value);
        var lvalue = await leftStore.AddOrUpdate(lkey, (k) => value, (k, v) => value);
        var rvalue = await rightStore.FindByIdAsync(lkey);

        await Handle(context, lkey, (lvalue, rvalue));
    }

    public async Task ExecuteRightAsync(KafkaContext context, K2 key, V2 value)
    {
        var leftStore = _getLeftStore(context);
        var rightStore = _getRightStore(context);

        var rkey = rightKey(key, value);
        var rvalue = await rightStore.AddOrUpdate(rkey, (k) => value, (k, v) => value);
        var lvalue = await leftStore.FindByIdAsync(rkey);

        await Handle(context, rkey, (lvalue, rvalue));
    }
}