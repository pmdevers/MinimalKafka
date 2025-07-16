using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream;

namespace MinimalKafka.Aggregates;


/// <summary>
/// 
/// </summary>
public static class AggregateExtensions
{
    /// <summary>
    /// Maps an aggregate command stream to an aggregate state stream using the specified topic,
    /// </summary>
    /// <typeparam name="TKey">The type of the aggregate key (identifier).</typeparam>
    /// <typeparam name="TCommand">The type of command applied to the aggregate.</typeparam>
    /// <typeparam name="TAggregate">The type representing the aggregate, which implements <see cref="IAggregate{TKey, TAggregate, TCommand}"/>.</typeparam>
    /// <param name="builder">The application builder providing access to the service container.</param>
    /// <param name="topicName">The name of the Kafka topic for aggregate commands and states.</param>
    /// <param name="commandSuffix">The suffix that is appended to the name for use of the command topic.</param>
    /// <param name="commandErrorSuffix">The suffix that is appended to the name for use of the command error topic.</param>
    /// <returns>
    /// An <see cref="IKafkaConventionBuilder"/> configured to process the aggregate command and state streams.
    /// </returns>
    public static IKafkaConventionBuilder MapAggregate<TAggregate, TKey, TCommand>(
        this IApplicationBuilder builder, 
        string topicName,
        string commandSuffix = "-commands",
        string commandErrorSuffix = "-errors")
        where TKey : notnull
        where TAggregate : IAggregate<TKey, TAggregate, TCommand>
        where TCommand : ICommand<TKey>
    {
        var sb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();
        return sb.MapAggregate<TAggregate, TKey, TCommand>(topicName, commandSuffix, commandErrorSuffix);
    }

    /// <summary>
    /// Maps an aggregate command stream to an aggregate state stream for the specified aggregate type.
    /// Handles aggregate initialization, version checking, state updates, and error publishing.
    /// </summary>
    /// <typeparam name="TKey">The type of the aggregate key (identifier).</typeparam>
    /// <typeparam name="TCommand">The type of command applied to the aggregate.</typeparam>
    /// <typeparam name="TAggregate">The type representing the aggregate, which implements <see cref="IAggregate{TKey, TAggregate, TCommand}"/>.</typeparam>
    /// <param name="builder">The Kafka builder used to configure the stream processing topology.</param>
    /// <param name="topicName">The logical topicName for the aggregate; used to derive topic names.</param>
    /// <param name="commandSuffix">The suffix that is appended to the topicName for use of the command topic.</param>
    /// <param name="commandErrorSuffix">The suffix that is appended to the topicName for use of the command error topic.</param>
    /// <returns>
    /// An <see cref="IKafkaConventionBuilder"/> configured to process the aggregate command and state streams.
    /// </returns>
    public static IKafkaConventionBuilder MapAggregate<TAggregate, TKey, TCommand>(
        this IKafkaBuilder builder, 
        string topicName,
        string commandSuffix = "-commands",
        string commandErrorSuffix = "-errors"
    )
        where TKey : notnull
        where TAggregate : IAggregate<TKey, TAggregate, TCommand>
        where TCommand : ICommand<TKey>
    {
        return builder.MapStream<TKey, TCommand>($"{topicName}{commandSuffix}")
            .Join<TKey, TAggregate>(topicName).OnKey()
            .Into(async (c, key, join) =>
            {
                var (cmd, state) = join;

                // Ignore null commands or recursive processing of state topic
                if (cmd is null || c.TopicName == topicName)
                {
                    return;
                }

                // If state is null, initialize aggregate from command
                state ??= TAggregate.Create(key);

                // Version check
                if (cmd.Version != state.Version)
                {
                    await c.ProduceAsync(
                        $"{topicName}{commandErrorSuffix}", 
                        key, 
                        CommandResult.Create(Result.Failed(state, $"Invalid command version: {cmd.Version}, expected: {state?.Version ?? 0}"), cmd));
                    return;
                }

                // Apply command
                var result = TAggregate.Apply(state, cmd);

                // produce if command was succesfull
                if (result.IsSuccess)
                {
                    await c.ProduceAsync(topicName, result.State.Id, result.State);
                }
                else
                {
                    await c.ProduceAsync(
                        $"{topicName}{commandErrorSuffix}", 
                        key,
                        CommandResult.Create(result, cmd));
                }
            });
    }
}