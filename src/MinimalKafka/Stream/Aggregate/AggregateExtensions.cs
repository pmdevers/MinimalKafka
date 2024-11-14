using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;

namespace MinimalKafka.Stream;


public static class AggregateExtensions
{
    public static IKafkaConventionBuilder MapAggregate<TKey, TAggregate>(this IApplicationBuilder app, string commandTopic, string aggregateTopic, Action<IAggregateBuilder<TKey, TAggregate>> commands)
        where TAggregate : Aggregate<TKey>, IAggregate<TKey, TAggregate>
    {
        var builder = app.ApplicationServices.GetRequiredService<IKafkaBuilder>();
        return builder.MapAggregate(commandTopic, aggregateTopic, commands);
    }

    public static IKafkaConventionBuilder MapAggregate<TKey, TAggregate>(this IKafkaBuilder builder, string commandTopic, string aggregateTopic, Action<IAggregateBuilder<TKey, TAggregate>> commands)
        where TAggregate : Aggregate<TKey>, IAggregate<TKey, TAggregate>
        => builder.MapStream<TKey, AggregateCommand<TKey>>(commandTopic)
                  .Aggregate(aggregateTopic, commands);
}
