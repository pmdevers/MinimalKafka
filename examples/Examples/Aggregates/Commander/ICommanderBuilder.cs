namespace Examples.Aggregates.Commander;

public interface ICommanderBuilder<TKey, TState, TCommand, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    void Add(CommandHandlerConfig<TKey, TState, TCommand, TEvent> config);
}
