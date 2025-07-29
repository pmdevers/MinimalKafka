namespace Examples.Aggregates;

public interface IAggregate<TKey>
    where TKey : notnull
{
    TKey Key { get; init; }
}

