using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.EventSourced.Generic;

public static class StreamBuilderExtensions
{

    public static void MapEventSourced<TKey, TState, TCommand, TCommandTypes, TEvent, TEventTypes>(
        this IApplicationBuilder builder, 
        Action<IBranchBuilder<TKey, (TCommand?, TState?)>> commandProcessor,
        Action<IBranchBuilder<TKey, (TState?, TEvent?)>> eventProcessor
    )
        where TCommand : ICommand<TKey, TCommandTypes>
        where TEvent : IEvent<TKey, TEventTypes>
        where TState : IState<TKey>
        where TKey : notnull
    {
        builder.MapStream<TKey, TCommand>("aggregate-commands")
            .Join<TKey, TState>("aggregate-state").OnKey()
            .SplitInto(commandProcessor);

        builder.MapStream<TKey, TState>("aggregate-state")
            .Join<TKey, TEvent>("aggregate-events").OnKey()
            .SplitInto(eventProcessor);
    }


    public static IBranchBuilder<TKey, (TState?, TEvent?)> ApplyEvent<TKey, TState, TEvent, TEventTypes>(
        this IBranchBuilder<TKey, (TState?, TEvent?)> builder,
        TEventTypes eventTypes,
        Func<TState, TEvent, TState> action
    )
        where TEvent : IEvent<TKey, TEventTypes>
        where TState : IState<TKey>
        where TKey : notnull
    {
        return builder.Branch((k, v) => 
            v.Item2 is TEvent evt && EqualityComparer<TEventTypes>.Default.Equals(evt.Type, eventTypes),
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

                await c.ProduceAsync("aggregate-state", k, newState); // Output the created event
            });
    }

    public static IBranchBuilder<TKey, (TCommand?, TState?)> HandleCommand<TKey, TCommand, TCommandType, TState, TEventTypes>
    (
        this IBranchBuilder<TKey, (TCommand?, TState?)> builder, 
        TCommandType commandType,
        Func<TState, TCommand, IEvent<TKey, TEventTypes>[]> action
    )
        where TKey : notnull
        where TCommand : ICommand<TKey, TCommandType>
        where TState: IState<TKey>, new()
    {
        return builder.Branch((k, v) =>
            v.Item1 is TCommand evt && EqualityComparer<TCommandType>.Default.Equals(evt.Type, commandType),
            async (c, k, v) =>
        {
            var (command, state) = v;
            if (state == null)
            {
                state = new TState(); // Initialize state if null
            }

            if(command == null)
            {
                // Ignore null commands
                return;
            }

            var events = action(state, command);
            foreach (var @event in events)
            {
                await c.ProduceAsync("aggregate-events", k, @event); // Output the created event
            }
        });
    }
}

