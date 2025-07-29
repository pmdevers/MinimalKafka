namespace Examples.Aggregates.Commander;

public interface ITypedCommand<TKey, TCommandTypes> : ICommand<TKey>
    where TKey : notnull
    where TCommandTypes : struct, Enum
{
    TCommandTypes Type { get; init; }
}
