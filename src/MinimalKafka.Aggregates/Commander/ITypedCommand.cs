namespace MinimalKafka.Aggregates.Commander;

/// <summary>
/// this interface defines a command that has a specific type.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TCommandTypes"></typeparam>
public interface ITypedCommand<out TKey, TCommandTypes> : ICommand<TKey>
    where TKey : notnull
    where TCommandTypes : struct, Enum
{
    /// <summary>
    /// the type of the command, which is an enum value.
    /// </summary>
    TCommandTypes Type { get; init; }
}
