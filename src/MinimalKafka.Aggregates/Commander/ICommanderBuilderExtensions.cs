namespace MinimalKafka.Aggregates.Commander;

/// <summary>
/// this class provides extension methods for configuring command handlers in an aggregate.
/// </summary>
public static class ICommanderBuilderExtensions
{
    /// <summary>
    /// this method allows you to specify a command type for which the command handler should be triggered.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TCommandType"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="builder"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static ICommanderThenBuilder<TKey, TState, TCommand, TEvent> When<TKey, TState, TCommand, TCommandType, TEvent>(

        this ICommanderBuilder<TKey, TState, TCommand, TEvent> builder, TCommandType type)

        where TKey : notnull
        where TState : IAggregate<TKey>, new()
        where TCommand : ITypedCommand<TKey, TCommandType>
        where TCommandType : struct, Enum
        where TEvent : IEvent<TKey>
    {
        return builder.When(x => EqualityComparer<TCommandType>.Default.Equals(x.Type, type));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="builder"></param>
    /// <param name="when"></param>
    /// <returns></returns>
    public static ICommanderThenBuilder<TKey, TState, TCommand, TEvent> When<TKey, TState, TCommand, TEvent>(

        this ICommanderBuilder<TKey, TState, TCommand, TEvent> builder, Func<TCommand, bool> when)

        where TKey : notnull
        where TState : IAggregate<TKey>, new()
        where TCommand : ICommand<TKey>
        where TEvent : IEvent<TKey>
    {
        var config = new CommandHandlerConfig<TKey, TState, TCommand, TEvent>(when);
        builder.Add(config);
        return new CommanderConfigBuilder<TKey, TState, TCommand, TEvent>(builder, config);
    }
}
