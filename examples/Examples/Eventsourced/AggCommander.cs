using Examples.Eventsourced.Commands;
using Examples.Eventsourced.Events;
using MinimalKafka.Aggregates;
using MinimalKafka.Aggregates.Applier;
using MinimalKafka.Aggregates.Commander;

namespace Examples.Eventsourced;

public class AggCommander : ICommander<Guid, Agg, Command, Event>
{
    public static void Configure(ICommanderBuilder<Guid, Agg, Command, Event> builder)
    {
        builder.When(CommandTypes.Create).Then(Create);
    }

    internal static Result<Event[]> Create(Agg state, Command c)
    {
        if (c.Create is null)
        {
            return Result.Failed(Array.Empty<Event>(), "Command");
        }

        var @event = new Event()
        {
            StreamId = c.StreamId,
            Type = EventTypes.Created,
            Created = new CreatedEvent(c.Create.Name)
        };

        return new Event[] { @event };
    }
}
