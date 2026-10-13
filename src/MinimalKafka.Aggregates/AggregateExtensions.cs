using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Aggregates.Helpers;
using MinimalKafka.Stream;

namespace MinimalKafka.Aggregates;

/// <summary>
/// Extension methods for mapping aggregate command and state streams.
/// </summary>
public static class AggregateExtensions
{
    extension(IApplicationBuilder builder)
    {
        /// <summary>
        /// Maps an aggregate command stream to an aggregate state stream for the specified aggregate type.
        /// Handles aggregate initialization, version checking, state updates, and error publishing.
        /// </summary>
        /// <typeparam name="TKey">The type of the aggregate key.</typeparam>
        /// <typeparam name="TCommand">The type of command applied to the aggregate.</typeparam>
        /// <typeparam name="TAggregate">The aggregate state type.</typeparam>
        /// <param name="topicName">The logical topic name for aggregate state.</param>
        /// <param name="commandSuffix">The suffix appended to the topic name for the command topic.</param>
        /// <param name="commandErrorSuffix">The suffix appended to the topic name for the command error topic.</param>
        public StreamJoinBuilder<TKey, TCommand, TKey, TAggregate>.KeyJoinedStreamBuilder MapAggregate<TAggregate, TKey, TCommand>(
            string topicName,
            string commandSuffix = "-commands",
            string commandErrorSuffix = "-errors")
            where TKey : notnull
            where TAggregate : IAggregate<TKey, TAggregate, TCommand>
            where TCommand : ICommand<TKey>
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

            var commandTopic = $"{topicName}{commandSuffix}";
            var commandErrorTopic = $"{topicName}{commandErrorSuffix}";
            var aggregateStream = builder
                .MapStream<TKey, TCommand>(commandTopic)
                .Join<TKey, TAggregate>(topicName)
                .OnKey();

            aggregateStream.Into(async (context, key, joinResult) =>
            {
                var (cmd, state) = joinResult;
                var logger = context.RequestServices.GetRequiredService<ILogger<TAggregate>>();

                if (cmd is null || context.TopicName == topicName)
                {
                    return;
                }

                state ??= TAggregate.Create(key);

                if (cmd.Version != state.Version)
                {
                    logger.VersionMismatch(cmd.GetType().Name, cmd.Version, state.Version, typeof(TAggregate).Name);

                    await context.ProduceAsync(
                        commandErrorTopic,
                        key,
                        CommandResult.Create(
                            Result.Failed(state, $"Invalid command version: {cmd.Version}, expected: {state.Version}"),
                            cmd));

                    return;
                }

                var result = TAggregate.Apply(state, cmd);

                if (result.IsSuccess)
                {
                    if (result.State.Version == cmd.Version)
                    {
                        logger.VersionNotChanged(cmd.GetType().Name, cmd.Version, typeof(TAggregate).Name);
                        return;
                    }

                    logger.CommandApplied(cmd.GetType().Name, cmd.Version, typeof(TAggregate).Name);
                    await context.ProduceAsync(topicName, result.State.Id, result.State);
                    return;
                }

                logger.CommandError(cmd.GetType().Name, cmd.Version, typeof(TAggregate).Name);
                await context.ProduceAsync(
                    commandErrorTopic,
                    key,
                    CommandResult.Create(result, cmd));
            });

            return aggregateStream;
        }
    }
}
