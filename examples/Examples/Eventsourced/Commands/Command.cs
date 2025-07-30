using MinimalKafka.Aggregates.Commander;

namespace Examples.Eventsourced.Commands;

public class Command : ITypedCommand<Guid, CommandTypes>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid StreamId { get; init; } = Guid.NewGuid();
    public int Version { get; init; }
    public required CommandTypes Type { get; init; }
    public CreateCommand? Create { get; set; }
}
