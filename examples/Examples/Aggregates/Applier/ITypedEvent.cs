namespace Examples.Aggregates.Applier;

public interface ITypedEvent<TKey, TEventTypes> : IEvent<TKey>
    where TKey : notnull
    where TEventTypes : struct, Enum
{
    TEventTypes Type { get; set; }
}

