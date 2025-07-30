using MinimalKafka.Aggregates.Commander;

namespace Examples.Aggregate;

public class TestCommands : ITypedCommand<Guid, Commands>
{
    public required Guid Id { get; init; } = Guid.NewGuid();
    public required Guid StreamId { get; init; } = Guid.NewGuid();
    public required int Version { get; init; }
    public required Commands Type { get; init; }
    public SetCounter? SetCounter { get; set; }

}

public record SetCounter(int Counter);
