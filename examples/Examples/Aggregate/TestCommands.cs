using MinimalKafka.Aggregates;

namespace Examples.Aggregate;

public class TestCommands : IAggregateCommands<Guid>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required int Version { get; init; }
    public required string CommandName { get; init; }
    public SetCounter? SetCounter { get; set; }

}

public record SetCounter(int Counter);
