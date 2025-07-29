namespace Examples.Aggregates.Commander;

public static class ICommanderBuilderExtensions
{
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
