namespace MinimalKafka.Aggregates.Commander;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TState"></typeparam>
/// <typeparam name="TCommand"></typeparam>
/// <typeparam name="TEvent"></typeparam>
public interface ICommanderThenBuilder<TKey, TState, TCommand, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    ICommanderBuilder<TKey, TState, TCommand, TEvent> Then(Func<TState, TCommand, Result<TEvent[]>> handler);
}