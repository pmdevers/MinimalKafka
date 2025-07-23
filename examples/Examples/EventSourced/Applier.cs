using Microsoft.Extensions.Logging;
using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.EventSourced;

public static class EventApplier
{
    public static void Apply(IBranchBuilder<Guid, (State<Guid>?, Event<Guid>?)> builder)
    {
        builder.ApplyEvent(EventTypes.Created, ApplyCreated);
    }

    private static State<Guid> ApplyCreated(State<Guid> state, Event<Guid> @event)
    {
        return state with
        {
            Id = @event.Id,
        };
    }
}
