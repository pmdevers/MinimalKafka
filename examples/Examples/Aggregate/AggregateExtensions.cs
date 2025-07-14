using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.Aggregate;

public static class AggregateExtensions
{
    /// <summary>
    /// Maps an aggregate command stream to an aggregate state stream.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TAgrregate"></typeparam>
    /// <param name="builder"></param>
    /// <param name="topic"></param>
    /// <returns></returns>
    public static IKafkaConventionBuilder MapAggregate<TKey, TCommand, TAgrregate>(this IApplicationBuilder builder, string topic)
        where TKey : notnull
        where TAgrregate : IAggregate<TKey, TAgrregate, TCommand>
        where TCommand : IAggregateCommands<TKey>
    {
        var sb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();
        return sb.MapAggregate<TKey, TCommand, TAgrregate>(topic);
    }

    /// <summary>
    /// Maps an aggregate command stream to an aggregate state stream.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IKafkaConventionBuilder MapAggregate<TKey, TCommand, TAgrregate>(this IKafkaBuilder builder, string name)
        where TKey : notnull
        where TAgrregate : IAggregate<TKey, TAgrregate, TCommand>
        where TCommand : IAggregateCommands<TKey>
    {
        return builder.MapStream<TKey, TCommand>($"{name}-commands")
            .Join<TKey, TAgrregate>(name).OnKey()
            .Into(async (c, key, join) =>
            {
                var (cmd, state) = join;

                if (cmd is null || c.ConsumerKey.TopicName == name)
                {
                    return;
                }

                state ??= TAgrregate.Create(cmd);

                if (cmd.Version != state.Version)
                {
                    await c.ProduceAsync($"{name}-errors", 
                        key, 
                        CommandResult.Create(Result.Failed(state, $"Invalid command version: {cmd.Version}, expected: {state.Version}"), cmd));
                    return;
                }

                var result = TAgrregate.Apply(state, cmd);

                if (result.IsSuccess)
                {
                    await c.ProduceAsync(name, key, result.State);
                }
                else
                {
                    await c.ProduceAsync($"{name}-errors", key,
                        CommandResult.Create(result, cmd));
                }
            });
    }
}

internal class CommandResult
{
    public static CommandResult<T, TCmd> Create<T, TCmd>(Result<T> result, TCmd command)
        => new()
        {
            Command = command,
            State = result.State,
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.ErrorMessage,
        };
}

internal class CommandResult<TState, TCommand> : Result<TState>
{
    public required TCommand Command { get; init; }

}