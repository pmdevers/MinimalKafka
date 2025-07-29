namespace Examples.Aggregates.Applier;

public class EventHandlerConfig<TKey, TState, TEvent>(Func<TEvent, bool> when)
    where TKey : notnull
    where TState : IAggregate<TKey>
    where TEvent : IEvent<TKey>
{
    public Func<TEvent, bool> When { get; set; } = when;
    public Func<TState, TEvent, Result<TState>> Then { get;set; } = (s, _) => s;
};

