using MinimalKafka.Aggregates.Applier;

namespace Examples.Eventsourced.Events;

public class Event : ITypedEvent<Guid, EventTypes>
{
    public Guid Id { get; init; } = Guid.NewGuid(); 
    public Guid StreamId { get; init; }
    public EventTypes Type { get; init; }
    public CreatedEvent? Created { get; set; }
}
