namespace Examples.EventSourced;

public enum EventTypes
{
    Created,
    Updated,
    Deleted,
    None
}

public class Event<TKey>
{
    public TKey Id { get; init; }
    public EventTypes Type { get; init; }
}
