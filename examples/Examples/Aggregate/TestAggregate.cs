namespace Examples.Aggregate;

public class TestCommands : IAggregateCommands<Guid>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required int Version { get; init; }
    public required string CommandName { get; init; }

    public SetCounter? SetCounter { get; set; }

}

public record SetCounter(int Counter);


public record Test : IAggregate<Guid, Test, TestCommands>
{
    public Guid Id { get; init; }
    public int Version { get; init; }
    public int Counter { get; init; }

    public static Result<Test> Create(TestCommands command)
        => new Test() { Id = command.Id, Version = 0 };

    public static Result<Test> Apply(Test state, TestCommands command)
    {
        var result = command.CommandName switch
        {
            nameof(Create) => Create(command),
            nameof(Increment) => state.Increment(),
            nameof(Decrement) => state.Decrement(),
            nameof(SetCounter) => state.SetCounter(command.SetCounter!),
            _ => Result.Failed(state, "Unknown command: " + command.CommandName)
        };

        if (result.IsSuccess)
        {
            return result.State with { Version = state.Version + 1 };
        }

        return result;
    }

    public Result<Test> Increment()
    {
        if (Counter >= 100)
        {
            return Result.Failed(this, "Counter cannot exceed 100.");
        }

        return this with
        {
            Counter = Counter + 1
        };
    }

    public Result<Test> Decrement()
    {
        if (Counter <= 0)
        {
            return Result.Failed(this, "Counter cannot be less than 0.");
        }

        return this with
        {
            Counter = Counter - 1
        };
    }

    public Result<Test> SetCounter(SetCounter cmd) 
    {
        if(cmd.Counter < 0)
        {
            return Result.Failed(this, "Counter cannot be less than 0.");
        }

        if(cmd.Counter > 100)
        {
            return Result.Failed(this, "Counter connot be more then 100.");
        }

        return this with
        {
            Counter = cmd.Counter
        };
    }
}
