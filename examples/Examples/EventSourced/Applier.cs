using Examples.EventSourced.Generic;
using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.EventSourced;

public static class EventApplier
{
    internal static void Apply(IBranchBuilder<Guid, (TestState?, TestEvent?)> builder)
    {
        builder
            .ApplyEvent(EventTypes.Created, ApplyCreated)
            .ApplyEvent(EventTypes.Updated, ApplyUpdated)
            .ApplyEvent(EventTypes.Deleted, ApplyDeleted)
            .DefaultBranch((c, k, v) => c.ProduceAsync("error", k, v.Item2));
    }

    private static TestState ApplyDeleted(TestState state, TestEvent @event)
    {
        throw new NotImplementedException();
    }

    private static TestState ApplyUpdated(TestState state, TestEvent @event)
    {
        throw new NotImplementedException();
    }

    private static TestState ApplyCreated(TestState state, TestEvent @event)
    {
        return new TestState
        {
            Id = @event.Id
        };
    }
}
