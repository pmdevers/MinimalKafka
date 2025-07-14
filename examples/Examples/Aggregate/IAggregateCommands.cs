namespace Examples.Aggregate;

public interface IAggregateCommands<TKey>
{
    TKey Id { get; }
    int Version { get; }
    string CommandName { get; }
}
