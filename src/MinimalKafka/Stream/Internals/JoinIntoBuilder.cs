using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;

namespace MinimalKafka.Stream.Internals;

internal class JoinIntoBuilder<K1, V1, K2, V2>(
    IKafkaBuilder builder,
    string leftTopic, 
    string rightTopic,
    Func<V1, V2, bool> on)
    : IIntoBuilder<(V1, V2)>
{
    private readonly Func<KafkaContext, IStreamStore<K1, V1>> _getLeftStore = (KafkaContext context)
        => context.RequestServices.GetRequiredService<IStreamStore<K1, V1>>();

    private readonly Func<KafkaContext, IStreamStore<K2, V2>> _getRightStore = (KafkaContext context)
        => context.RequestServices.GetRequiredService<IStreamStore<K2, V2>>();

    private Func<KafkaContext, (V1, V2), Task> _into = (_, _) => Task.CompletedTask;

    public IKafkaConventionBuilder Into(Func<KafkaContext, (V1, V2), Task> handler)
    {
        _into = handler;

        var l = builder.MapTopic(leftTopic, ExecuteLeftAsync);
        var r = builder.MapTopic(rightTopic, ExecuteRightAsync);

        return new JoinConventionBuilder(l, r);
    }

    public async Task ExecuteLeftAsync(KafkaContext context, K1 key, V1 value)
    {
        var leftStore = _getLeftStore(context);
        var rightStore = _getRightStore(context);

        var left = await leftStore.AddOrUpdate(key, (k) => value, (k, v) => value);
       
        await foreach (var right in rightStore.FindAsync(x => on(value, x)))
        { 
            await _into(context, (left, right));
        }
    }

    public async Task ExecuteRightAsync(KafkaContext context, K2 key, V2 value)
    {
        var leftStore = _getLeftStore(context);
        var rightStore = _getRightStore(context);

        var right = await rightStore.AddOrUpdate(key, (k) => value, (k, v) => value);
       
        await foreach (var left in leftStore.FindAsync(x => on(x, value)))
        {
            await _into(context, (left, right));
        }
    }
}
