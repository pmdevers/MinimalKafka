namespace Examples.Aggregates.Commander;

public interface ICommanderThenBuilder<TKey, TState, TCommand, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    ICommanderBuilder<TKey, TState, TCommand, TEvent> Then(Func<TState, TCommand, Result<TEvent[]>> handler);
}