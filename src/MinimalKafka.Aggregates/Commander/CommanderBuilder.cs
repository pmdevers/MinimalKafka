using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Aggregates.Storage;

namespace MinimalKafka.Aggregates.Commander;

internal class CommanderBuilder<TKey, TState, TCommand, TEvent>(string eventTopic)
    : ICommanderBuilder<TKey, TState, TCommand, TEvent>
    where TKey : notnull
    where TState : IAggregate<TKey>, new()
    where TCommand : ICommand<TKey>
    where TEvent : IEvent<TKey>
{
    private readonly ICollection<CommandHandlerConfig<TKey, TState, TCommand, TEvent>> _handlers = [];
    
    private Func<KafkaContext, TKey, TCommand, TState, Result<TEvent[]>, Task> _default
        = async (c, k, command, state, result) => {
            if (result.IsSuccess)
            {
                foreach (var @event in result.State)
                {
                    await c.ProduceAsync(eventTopic, k, @event);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        };

    public void Add(CommandHandlerConfig<TKey, TState, TCommand, TEvent> commandHandlerConfig)
    {
        _handlers.Add(commandHandlerConfig);
    }

    public void Change(Func<KafkaContext, TKey, TCommand, TState, Result<TEvent[]>, Task> handler)
    {
        _default = handler;
    }

    public Func<KafkaContext, TKey, TCommand, Task> Build()
    {
        return async (c, k, command) =>
        {
            if(command is null) 
            {
                return; 
            }
            
            var handler = _handlers.FirstOrDefault(x => x.When(command));

            if (handler is null) 
            {
                return;
            }

            var store = c.RequestServices.GetRequiredService<IAggregateStore<TKey, TState>>();
            var state = await store.FindByIdAsync(k);
            state ??= new() { Id = k };

            var result = handler.Then(state, command);

            await _default(c, k, command, state, result);
        };
    }
}