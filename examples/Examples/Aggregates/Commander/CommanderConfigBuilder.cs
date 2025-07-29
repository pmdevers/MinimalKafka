namespace Examples.Aggregates.Commander;

internal class CommanderConfigBuilder<TKey, TState, TCommand, TEvent>(
    ICommanderBuilder<TKey, TState, TCommand, TEvent> builder,
    CommandHandlerConfig<TKey, TState, TCommand, TEvent> config)

    : ICommanderThenBuilder<TKey, TState, TCommand, TEvent>

    where TKey : notnull
    where TState : IAggregate<TKey>, new()
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    public ICommanderBuilder<TKey, TState, TCommand, TEvent> Then(Func<TState, TCommand, Result<TEvent[]>> handler)
    {
        config.Then = handler;
        return builder;
    }
}
