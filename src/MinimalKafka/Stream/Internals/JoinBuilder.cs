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
    ConsumeBlock<K1, V1> left, string topic) : IJoinBuilder<V1, V2>
{
    private readonly ConsumeBlock<K1, V1> _left = left;
    private readonly ConsumeBlock<K2, V2> _right = new(builder, topic);

    public void Add(Action<IKafkaBuilder> convention)
    {
        _right.Builder.Add(convention);
    }

    public void Finally(Action<IKafkaBuilder> finalConvention)
    {
        _right.Builder.Finally(finalConvention);
    }

    public IIntoBuilder<(V1, V2)> On(Func<V1, V2, bool> on)
    {
        var leftStore = builder.ServiceProvider.GetRequiredService<IStreamStore<K1, V1>>();
        var rightStore = builder.ServiceProvider.GetRequiredService<IStreamStore<K2, V2>>();

        var join = new JoinBlock<K1, V1, K2, V2>(leftStore, rightStore, on);

        _left.LinkTo(join.Left);
        _right.LinkTo(join.Right);

        var joinBuilder = new JoinConventionBuilder(_left.Builder, _right.Builder);

        return new IntoBuilder<K1, V1, K2, V2>(joinBuilder, join);
    }
}
