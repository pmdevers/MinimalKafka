namespace MinimalKafka.Aggregates;

internal static class CommandResult
{
    public static CommandResult<T, TCmd> Create<T, TCmd>(Result<T> result, TCmd command)
        => new()
        {
            Command = command,
            State = result.State,
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.ErrorMessage,
        };
}

internal class CommandResult<TState, TCommand> : Result<TState>
{
    public required TCommand Command { get; init; }

}