using Examples.Eventsourced.Events;
using MinimalKafka.Aggregates;

namespace Examples.Eventsourced;

public record Agg() : IAggregate<Guid>
{
    public Guid Id { get; init; }

    public int Version { get; init; }

    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}