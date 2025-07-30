namespace MinimalKafka.Aggregates.Commander;

/// <summary>
/// this interface defines a builder for configuring command handlers in an aggregate.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TCommand"></typeparam>
/// <typeparam name="TEvent"></typeparam>
public interface ICommanderBuilder<TKey, TState, TCommand, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// this method builds the command handler function that will be used to process commands.
    /// </summary>
    /// <param name="commandHandlerConfig"></param>
    void Add(CommandHandlerConfig<TKey, TState, TCommand, TEvent> commandHandlerConfig);
}
