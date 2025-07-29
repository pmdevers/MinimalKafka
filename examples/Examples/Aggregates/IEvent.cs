namespace Examples.Aggregates;

public interface IEvent<TKey>
    where TKey : notnull
{
    TKey Key { get; }
}

