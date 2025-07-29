namespace Examples.Aggregates.Commander;

public class CommandHandlerConfig<TKey, TState, TCommand, TEvent>(Func<TCommand, bool> when)
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    public Func<TCommand, bool> When { get; } = when;
    public Func<TState, TCommand, Result<TEvent[]>> Then { get; set; } = 
        (_, _) => Result.Failed(Array.Empty<TEvent>(), "Handler not implemented!");
}
