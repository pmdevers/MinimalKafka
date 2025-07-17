namespace MinimalKafka.Aggregates;

/// <summary>
/// Static helper class for creating <see cref="CommandResult{TState, TCommand}"/> objects representing command execution outcomes.
/// </summary>
public static class CommandResult
{
    /// <summary>
    /// Creates a <see cref="CommandResult{TState, TCommand}"/> from a <see cref="Result{T}"/> and a command.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TCmd"></typeparam>
    /// <param name="result"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static CommandResult<T, TCmd> Create<T, TCmd>(Result<T> result, TCmd command)
        => new()
        {
            Command = command,
            State = result.State,
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.ErrorMessage,
        };
}


/// <summary>
/// Represents the outcome of execution of a command on state, including, command, state, success status, and error messages (if any).
/// </summary>
public class CommandResult<TState, TCommand> : Result<TState>
{
    /// <summary>
    /// The Command that was executed to produce this result.
    /// </summary>
    public required TCommand Command { get; init; }

}