namespace Examples.Aggregates.Commander;

public interface ICommander<TKey, TState, TCommand, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    abstract static void Configure(ICommanderBuilder<TKey, TState, TCommand, TEvent> builder);
}