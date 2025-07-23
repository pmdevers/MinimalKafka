using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.EventSourced;

public static class StreamBuilderExtensions
{

    public static void MapEventSourced(this IApplicationBuilder builder)
    {
        builder.MapStream<Guid, Command<Guid>>("command")
            .Join<Guid, State<Guid>>("state").OnKey()
            .SplitInto(CommandProcessor.Process);

        builder.MapStream<Guid, State<Guid>>("state")
            .Join<Guid, Event<Guid>>("event").OnKey()
            .SplitInto(EventApplier.Apply);
    }


    public static IBranchBuilder<TKey, (TState?, TEvent?)> ApplyEvent<TKey, TState, TEvent>(this IBranchBuilder<TKey, (TState?, TEvent?)> builder,
        EventTypes type,
        Func<TState, TEvent, TState> action)
        where TEvent : Event<TKey>
        where TState : State<TKey>
    {
        return builder.Branch((k, v) => v.Item2?.Type == type,
            async (c, k, v) =>
            {
                var (state, @event) = v;
                if (@event == null)
                {
                    return; // Ignore null events
                }
                if (state == null)
                {
                    // Handle the case where state is null
                    return;
                }
                var newState = action(state, @event);

                await c.ProduceAsync("topic", k, newState); // Output the created event
            });
    }

    public static IBranchBuilder<TKey, (TCommand?, TState?)> HandleCommand<TKey, TCommand, TState>(this IBranchBuilder<TKey, (TCommand?, TState?)> builder, 
        CommandType commandType,
        Func<TState, TCommand, Event<TKey>[]> action)
        where TCommand : Command<TKey>
        where TState: State<TKey>
    {
        return builder.Branch((k, v) => v.Item1?.Type == commandType, 
            async (c, k, v) =>
        {
            var (command, state) = v;
            if (state == null)
            {
                // Handle the case where state is null
                return;
            }

            if(command == null)
            {
                // Ignore null commands
                return;
            }

            var events = action(state, command);
            foreach (var @event in events)
            {
                await c.ProduceAsync("topic-events", k, @event); // Output the created event
            }
        });
    }
}

