using Examples.Aggregates.Applier;

namespace Examples.Aggregates;

public interface ICommand<TKey>
    where TKey : notnull
{
    TKey Key { get; }
}
