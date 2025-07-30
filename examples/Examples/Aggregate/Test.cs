using MinimalKafka.Aggregates;
using MinimalKafka.Aggregates.Applier;
using MinimalKafka.Aggregates.Commander;

namespace Examples.Aggregate;

public enum Commands
{
    Create,
    Increment,
    Decrement,
    SetCounter
}

public enum Events
{
    Created,
    Incremented,
    Decremented,
    CounterSet
}


public class TestEvents : ITypedEvent<Guid, Events>
{
    public Guid Id { get; init; }
    public Guid StreamId { get; init; }
    public Events Type { get; init; }
    public int? Increment { get; set; }
    public int? Decrement { get; set; }
    public int? Counter { get; set; }
}

/// <summary>
/// Example aggregate for testing purposes. Supports increment, decrement, and set operations on a counter.
/// </summary>
public record Test : IAggregate<Guid>, 
    ICommander<Guid, Test, TestCommands, TestEvents>,
    IEventApplier<Guid, Test, TestEvents>
{
    /// <summary>
    /// Gets the aggregate identifier.
    /// </summary>
    public Guid Id { get; init; }

    public int Version { get; init; }
    /// <summary>
    /// Gets the current counter value.
    /// </summary>
    public int Counter { get; private set; }

    public static Result<TestEvents[]> Create(Test state, TestCommands command)
    {
        return new TestEvents[] {
            new()
            {
                StreamId = command.StreamId,
                Type = Events.Created
            }
        };
    }

    /// <summary>
    /// Increments the counter by one.
    /// </summary>
    /// <returns>New state if successful, or failed result if out of bounds.</returns>
    public static Result<TestEvents[]> Increment(Test state, TestCommands command)
    {
        if (state.Counter >= 100)
        {
            return Result.Failed(Array.Empty<TestEvents>(), "Counter cannot exceed 100.");
        }

        return new TestEvents[] {
            new()
            {
                StreamId = command.StreamId,
                Type = Events.Incremented,
                Increment = state.Counter + 1
            }
        };
    }

    /// <summary>
    /// Decrements the counter by one.
    /// </summary>
    /// <returns>New state if successful, or failed result if out of bounds.</returns>
    public static Result<TestEvents[]> Decrement(Test state, TestCommands command)
    {
        if (state.Counter <= 0)
        {
            return Result.Failed(Array.Empty<TestEvents>(), "Counter cannot be less than 0.");
        }

        return new TestEvents[] {
            new()
            {
                StreamId = command.StreamId,
                Type = Events.Decremented,
                Decrement = state.Counter - 1
            }
        };
    }

    /// <summary>
    /// Sets the counter to a specific value using the <see cref="SetCounter"/> command.
    /// </summary>
    /// <param name="cmd">The command containing the new counter value.</param>
    /// <returns>New state if within bounds, or failed result otherwise.</returns>
    public static Result<TestEvents[]> SetCounter(Test state, TestCommands command)
    {
        if(command.SetCounter is null)
        {
            return Result.Failed(Array.Empty<TestEvents>(), "SetCounter command data is null");
        }

        if (command.SetCounter.Counter < 0)
        {
            return Result.Failed(Array.Empty<TestEvents>(), "Counter cannot be less than 0.");
        }

        if (command.SetCounter.Counter > 100)
        {
            return Result.Failed(Array.Empty<TestEvents>(), "Counter cannot be more than 100.");
        }

        return new TestEvents[] {
            new()
            {
                StreamId = command.StreamId,
                Type = Events.Decremented,
                Counter = command.SetCounter.Counter    
            }
        };
    }

    public static void Configure(ICommanderBuilder<Guid, Test, TestCommands, TestEvents> builder)
    {
        builder.When(Commands.Create).Then(Create)
               .When(Commands.Increment).Then(Increment)
               .When(Commands.Decrement).Then(Decrement)
               .When(Commands.SetCounter).Then(SetCounter);
    }

    public static void Configure(IApplierBuilder<Guid, Test, TestEvents> builder)
    {
        builder.When(Events.Created).Then(Created)
            .When(Events.Incremented).Then(Incemented)
            .When(Events.Decremented).Then(Decremented)
            .When(Events.CounterSet).Then(CounterSet);

    }

    private static Result<Test> CounterSet(Test test, TestEvents events)
    {
        if (events.Counter is null)
        {
            return Result.Failed(test, "CounterSet event does not contain a valid counter value.");
        }

        return test with
        {
            Counter = events.Counter.Value,
        };
    }

    private static Result<Test> Decremented(Test test, TestEvents events)
    {
        if(events.Decrement is null)
        {
            return Result.Failed(test, "Decrement event does not contain a valid decrement value.");
        }

        return test with
        {
            Counter = events.Decrement.Value,
        };
    }

    private static Result<Test> Incemented(Test test, TestEvents events)
    {
        if (events.Increment is null)
        {
            return Result.Failed(test, "Incremented event does not contain a valid Incremented value.");
        }

        return test with
        {
            Counter = events.Increment ?? 0,
        };
    }

    private static Result<Test> Created(Test test, TestEvents events)
    {
        return test with
        {
            Id = events.StreamId,
            Counter = 0,
        };
    }
}
