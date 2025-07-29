namespace Examples.Aggregates.Applier;

public static class IApplierBuilderExtensions 
{
    public static IApplierThenBuilder<TKey, TState, TEvent> When<TKey, TState, TEvent, TEventType>(
        
        this IApplierBuilder<TKey, TState, TEvent> builder, TEventType type)
        
        where TKey : notnull
        where TState : IAggregate<TKey>, new()
        where TEvent: ITypedEvent<TKey, TEventType>
        where TEventType : struct, Enum
    {
        return builder.When(x => EqualityComparer<TEventType>.Default.Equals(x.Type, type));
    }

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

