namespace MinimalKafka.Aggregates;


/// <summary>
/// Defines the contract for an aggregate in a domain-driven design (DDD) context,
/// with support for applying commands to produce new aggregate states.
/// </summary>
/// <typeparam name="TKey">The type of the aggregate's unique identifier.</typeparam>
/// <typeparam name="TState">The type representing the aggregate's state.</typeparam>
/// <typeparam name="TCommand">The type representing a command that can be applied to the aggregate.</typeparam>
public interface IAggregate<TKey, TState, TCommand>
    where TCommand : ICommand<TKey>
{
    /// <summary>
    /// Gets the unique identifier of the aggregate instance.
    /// </summary>
    TKey Id { get; init; }

    /// <summary>
    /// Gets the current version of the aggregate.
    /// Used for optimistic concurrency and event sourcing.
    /// </summary>
    int Version { get; init; }

    /// <summary>
    /// Applies the specified command to the given state and returns the resulting state.
    /// </summary>
    /// <param name="state">The current state of the aggregate.</param>
    /// <param name="command">The command to apply.</param>
    /// <returns>
    /// A <see cref="Result{TState}"/> containing the new state and success/error information.
    /// </returns>
    abstract static Result<TState> Apply(TState state, TCommand command);

    /// <summary>
    /// Creates a new aggregate state by applying the specified command.
    /// Typically used for initializing a new aggregate instance.
    /// </summary>
    /// <returns>
    /// A <see cref="Result{TState}"/> containing the initial state and success/error information.
    /// </returns>
    abstract static TState Create(TKey id);
}
