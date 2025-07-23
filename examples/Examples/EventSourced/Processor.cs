using Examples.EventSourced.Generic;
using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.EventSourced;



public class CommandProcessor
{
    internal static void Process(IBranchBuilder<Guid, (TestCommand?, TestState?)> builder)
    {
        builder
            .HandleCommand(CommandType.Create, Create)
            .HandleCommand(CommandType.Update, Update)
            .HandleCommand(CommandType.Delete, Delete)
            .DefaultBranch(async (c, k, v) => {
                await c.ProduceAsync("error", k, v.Item1);
             });
    }

    private static IEvent<Guid, EventTypes>[] Delete(TestState state, TestCommand command)
    {
        throw new NotImplementedException();
    }

    private static IEvent<Guid, EventTypes>[] Update(TestState state, TestCommand command)
    {
        throw new NotImplementedException();
    }

    private static IEvent<Guid, EventTypes>[] Create(TestState state, TestCommand command)
    {
        return
        [
            new TestEvent
            {
                Id = command.Id,
                Type = EventTypes.Created
            }
        ];
    }
}

