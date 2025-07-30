namespace MinimalKafka.Aggregates.Commander;

/// <summary>
/// this interface defines a commander for handling commands in an aggregate.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TCommand"></typeparam>
/// <typeparam name="TEvent"></typeparam>
public interface ICommander<TKey, TState, TCommand, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// this method is used to configure the commander with the given builder.
    /// </summary>
    /// <param name="builder"></param>
    abstract static void Configure(ICommanderBuilder<TKey, TState, TCommand, TEvent> builder);
}