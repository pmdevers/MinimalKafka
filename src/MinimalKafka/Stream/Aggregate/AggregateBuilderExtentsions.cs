using MinimalKafka.Builders;
using System.Data.Common;

namespace MinimalKafka.Stream;

public static class AggregateBuilderExtentsions
{
    public static IKafkaConventionBuilder Aggregate<TKey, TValue>(this IStreamBuilder<TKey, AggregateEvent<TKey>> builder, string topic,
        Action<IAggregateBuilder<TKey, TValue>> config)
        where TValue : Aggregate<TKey>, IAggregate<TKey, TValue>
    {
        var intoBuilder = builder.Join<TKey, TValue>(topic).OnKey();
        var build = new AggregateBuilder<TKey, TValue>(intoBuilder, topic);
        config(build);
        return build.Build();
    }
}
