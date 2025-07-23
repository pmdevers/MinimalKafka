namespace Examples.EventSourced.Generic;

public interface IEvent<TKey, TEventTypes>
{
    TKey Id { get; }
    TEventTypes Type { get; }
}
