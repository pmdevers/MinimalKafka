using Examples.Eventsourced.Events;
using MinimalKafka.Aggregates;
using MinimalKafka.Aggregates.Applier;
using MinimalKafka.Aggregates.Commander;

namespace Examples.Eventsourced;

public class AggApplier : IEventApplier<Guid, Agg, Event>
{
    public static void Configure(IApplierBuilder<Guid, Agg, Event> builder)
    {
        builder
            .When(EventTypes.Created).Then(Created)
            .When(EventTypes.Deleted).Then(Deleted);
    }


    internal static Result<Agg> Deleted(Agg aggregate, Event e)
    {
        if (e.Type != EventTypes.Deleted)
            return Result.Failed(aggregate, "EventType mismatch.");

        return aggregate with
        {
            IsDeleted = true,
        };
    }

    internal static Result<Agg> Created(Agg aggregate, Event e)
    {
        if (e.Created is null)
            return Result.Failed(aggregate, "EventBody missing.");

        return aggregate with
        {
            Name = e.Created.Name,
        };
    }
}
