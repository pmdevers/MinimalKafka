using Confluent.Kafka;
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


    public static Task<DeliveryResult<TKey, AggregateCommand<TKey>>> ProduceCommand<TKey, TCommand>(this KafkaContext context, string topic, TKey key, TCommand command)
        where TCommand : notnull
    {
        return context.ProduceAsync(topic, key, AggregateCommand.Create(key, command));
    }
}

public class CommandProducer<TKey>(IProducer<TKey, AggregateCommand<TKey>> producer)
{
    public void Produce<TCommand>(string topic, TKey key, TCommand command)
        where TCommand : notnull   
    {
        producer.Produce(topic, new()
        {
            Key = key,
            Value = AggregateCommand.Create(key, command)
        });
    }

    public async Task ProduceAsync<TCommand>(string topic, TKey key, TCommand command)
        where TCommand : notnull
    {
        await producer.ProduceAsync(topic, new()
        {
            Key = key,
            Value = AggregateCommand.Create(key, command)
        });
    }
}