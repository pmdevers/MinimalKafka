namespace MinimalKafka.Aggregates.Applier;


/// <summary>
/// this class provides extension methods for the <see cref="IApplierBuilder{TKey, TState, TEvent}"/> interface.
/// </summary>
public static class IApplierBuilderExtensions 
{
    /// <summary>
    /// registers an event handler configuration with a specific event type.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventType"></typeparam>
    /// <param name="builder"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IApplierThenBuilder<TKey, TState, TEvent> When<TKey, TState, TEvent, TEventType>(
        
        this IApplierBuilder<TKey, TState, TEvent> builder, TEventType type)
        
        where TKey : notnull
        where TState : IAggregate<TKey>, new()
        where TEvent: ITypedEvent<TKey, TEventType>
        where TEventType : struct, Enum
    {
        return builder.When(x => EqualityComparer<TEventType>.Default.Equals(x.Type, type));
    }

    /// <summary>
    /// Registers an event handler configuration with a condition to filter events based on a predicate.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <param name="builder"></param>
    /// <param name="when"></param>
    /// <returns></returns>
    public static IApplierThenBuilder<TKey, TState, TEvent> When<TKey, TEvent, TState>(
        
        this IApplierBuilder<TKey, TState, TEvent> builder, Func<TEvent, bool> when)
        
        where TKey : notnull
        where TState : IAggregate<TKey>, new()
        where TEvent : IEvent<TKey>
    {
        var config = new EventHandlerConfig<TKey, TState, TEvent>(when);
        builder.Add(config);
        return new ApplierConfigBuilder<TKey, TState, TEvent>(builder, config);
    }
}

