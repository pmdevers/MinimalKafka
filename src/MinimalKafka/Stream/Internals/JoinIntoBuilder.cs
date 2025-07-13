using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Stream.Internals;

internal sealed class JoinIntoBuilder<K1, V1, K2, V2>(
    IKafkaBuilder builder,
    string leftTopic,
    string rightTopic,
    Func<V1, V2, bool> on)
    : IIntoBuilder<(V1, V2)>
    where K1 : notnull
    where K2 : notnull
{
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
        var rightStore = context.GetTopicStore(rightTopic);

        await foreach (var right in rightStore.FindAsync<V2>(x => on(value, x)))
        {
            await _into(context, (value, right));
        }
    }

    public async Task ExecuteRightAsync(KafkaContext context, K2 key, V2 value)
    {
        var leftStore = context.GetTopicStore(leftTopic);

        await foreach (var left in leftStore.FindAsync<V1>(x => on(x, value)))
        {
            await _into(context, (left, value));
        }
    }
}
