using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream.Blocks;
using System.Threading.Tasks.Dataflow;

namespace MinimalKafka.Stream.Internals;

internal class JoinConventionBuilder(
    IKafkaConventionBuilder left, 
    IKafkaConventionBuilder right) : IKafkaConventionBuilder
{
    public void Add(Action<IKafkaBuilder> convention)
    {
        left.Add(convention);
        right.Add(convention);
    }

    public void Finally(Action<IKafkaBuilder> finalConvention)
    {
        left.Finally(finalConvention);
        right.Finally(finalConvention);
    }
}


internal class JoinBuilder<K1, V1, K2, V2>(IKafkaBuilder builder,
    ConsumeBlock<K1, V1> left, string topic) : IJoinBuilder<K1, V1, K2, V2>
{
    private readonly ConsumeBlock<K1, V1> _left = left;
    private readonly ConsumeBlock<K2, V2> _right = new(builder, topic);

    public IIntoBuilder<(V1, V2)> On(Func<V1, V2, bool> on)
    {
        var leftStore = builder.ServiceProvider.GetRequiredService<IStreamStore<K1, V1>>();
        var rightStore = builder.ServiceProvider.GetRequiredService<IStreamStore<K2, V2>>();

        var join = new JoinBlock<K1, V1, K2, V2>(leftStore, rightStore, on);

        _left.LinkTo(join.Left, new DataflowLinkOptions() {  PropagateCompletion = true });
        _right.LinkTo(join.Right, new DataflowLinkOptions() { PropagateCompletion = true });

        var joinBuilder = new JoinConventionBuilder(_left.Builder, _right.Builder);

        return new IntoBuilder<(V1, V2)>(joinBuilder, join);
    }

    public IIntoBuilder<TKey, (V1?, V2?)> On<TKey>(Func<K1, V1, TKey> leftKey, Func<K2, V2, TKey> rightKey)
    {
        var store = builder.ServiceProvider.GetRequiredService<IStreamStore<TKey, (V1?, V2?)>>();

        var join = new InnerJoinBlock<TKey, K1, V1, K2, V2>(store, leftKey, rightKey);

        _left.LinkTo(join.Left, new DataflowLinkOptions() { PropagateCompletion = true });
        _right.LinkTo(join.Right, new DataflowLinkOptions() { PropagateCompletion = true });

        var joinBuilder = new JoinConventionBuilder(_left.Builder, _right.Builder);

        return new IntoBuilder<TKey, (V1?, V2?)>(joinBuilder, join);
    }
}
