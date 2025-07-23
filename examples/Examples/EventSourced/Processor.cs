using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.EventSourced;



public class CommandProcessor
{
    public static void Process<TKey>(IBranchBuilder<TKey, (Command<TKey>?, State<TKey>?)> builder)
    {
        builder
            .HandleCommand(CommandType.Create, Create)
            .HandleCommand(CommandType.Update, Update)
            .HandleCommand(CommandType.Delete, Delete)
            .DefaultBranch((c, k, v) => c.ProduceAsync("error", k, v.Item1));
    }

    private static Event<TKey>[] Delete<TKey>(State<TKey> state, Command<TKey> command)
    {
        return [
            new Event<TKey>
            {
                Id = state.Id,
                Type = EventTypes.Created,
            }
        ];
    }

    private static Event<TKey>[] Update<TKey>(State<TKey> state, Command<TKey> command)
    {
        throw new NotImplementedException();
    }

    private static Event<TKey>[] Create<TKey>(State<TKey> state, Command<TKey> command)
    {
        throw new NotImplementedException();
    }
}

