using MinimalKafka.Aggregates;

namespace Examples.Aggregate;

public class TestCommands : ICommand<Guid>
{
    public required Guid Id { get; init; } = Guid.NewGuid();
    public required int Version { get; init; }
    public required Commands Command { get; init; }
    public SetCounter? SetCounter { get; set; }

}

public record SetCounter(int Counter);
