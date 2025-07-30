namespace MinimalKafka.Aggregates.Commander;

/// <summary>
/// a configuration class for command handlers in an aggregate.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TCommand"></typeparam>
/// <typeparam name="TEvent"></typeparam>
/// <param name="when"></param>
public class CommandHandlerConfig<TKey, TState, TCommand, TEvent>(Func<TCommand, bool> when)
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// the condition that triggers this handler.
    /// </summary>
    public Func<TCommand, bool> When { get; } = when;

    /// <summary>
    /// the function that handles the command and returns the events to be produced.
    /// </summary>
    public Func<TState, TCommand, Result<TEvent[]>> Then { get; set; } = 
        (_, _) => Result.Failed(Array.Empty<TEvent>(), "Handler not implemented!");
}
