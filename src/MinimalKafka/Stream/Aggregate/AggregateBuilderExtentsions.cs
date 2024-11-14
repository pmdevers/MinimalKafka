using MinimalKafka.Builders;
using System.Data.Common;

namespace MinimalKafka.Stream;

public static class AggregateBuilderExtentsions
{
    public static IKafkaConventionBuilder Aggregate<TKey, TAggregate>(this IStreamBuilder<TKey, AggregateCommand<TKey>> builder, string topic,
        Action<IAggregateBuilder<TKey, TAggregate>> config)
        where TAggregate : Aggregate<TKey>, IAggregate<TKey, TAggregate>
    {
        var intoBuilder = builder.Join<TKey, TAggregate>(topic).OnKey();
        var build = new AggregateBuilder<TKey, TAggregate>(intoBuilder, topic);
        config(build);
        return build.Build();
    }
}
