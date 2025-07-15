using MinimalKafka.Aggregates;

namespace Examples.Aggregate;

public enum Commands
{
    Create,
    Increment,
    Decrement,
    SetCounter
}

/// <summary>
/// Example aggregate for testing purposes. Supports increment, decrement, and set operations on a counter.
/// </summary>
public record Test : IAggregate<Guid, Test, TestCommands>
{
    /// <summary>
    /// Gets the aggregate identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the aggregate version.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Gets the current counter value.
    /// </summary>
    public int Counter { get; init; }

    /// <summary>
    /// Creates a new instance of <see cref="Test"/> aggregate from a command.
    /// </summary>
    /// <param name="command">The command to initialize the aggregate.</param>
    /// <returns>A new <see cref="Test"/> aggregate wrapped in a <see cref="Result{Test}"/>.</returns>
    public static Test Create(Guid id)
        => new() { Id = id };

    /// <summary>
    /// Applies a command to the current state and returns the resulting state.
    /// </summary>
    /// <param name="state">The current aggregate state.</param>
    /// <param name="command">The command to apply.</param>
    /// <returns>The new state as a <see cref="Result{Test}"/>.</returns>
    public static Result<Test> Apply(Test state, TestCommands command)
    {
        var result = command.Command switch
        {
            Commands.Create => state.Create(),
            Commands.Increment => state.Increment(),
            Commands.Decrement => state.Decrement(),
            Commands.SetCounter => command.SetCounter != null
                ? state.SetCounter(command.SetCounter)
                : Result.Failed(state, "SetCounter command data is null"),
            _ => Result.Failed(state, "Unknown command: " + command.Command)
        };

        if (result.IsSuccess)
        {
            return result.State with { Version = state.Version + 1 };
        }

        return result;
    }

    public Result<Test> Create()
    {
        return this with { 
            Counter = 0 
        };
    }

    /// <summary>
    /// Increments the counter by one.
    /// </summary>
    /// <returns>New state if successful, or failed result if out of bounds.</returns>
    public Result<Test> Increment()
    {
        if (Counter >= 100)
        {
            return Result.Failed(this, "Counter cannot exceed 100.");
        }

        return this with { Counter = Counter + 1 };
    }

    /// <summary>
    /// Decrements the counter by one.
    /// </summary>
    /// <returns>New state if successful, or failed result if out of bounds.</returns>
    public Result<Test> Decrement()
    {
        if (Counter <= 0)
        {
            return Result.Failed(this, "Counter cannot be less than 0.");
        }

        return this with { Counter = Counter - 1 };
    }

    /// <summary>
    /// Sets the counter to a specific value using the <see cref="SetCounter"/> command.
    /// </summary>
    /// <param name="cmd">The command containing the new counter value.</param>
    /// <returns>New state if within bounds, or failed result otherwise.</returns>
    public Result<Test> SetCounter(SetCounter cmd)
    {
        if (cmd.Counter < 0)
        {
            return Result.Failed(this, "Counter cannot be less than 0.");
        }

        if (cmd.Counter > 100)
        {
            return Result.Failed(this, "Counter cannot be more than 100.");
        }

        return this with { Counter = cmd.Counter };
    }
}
