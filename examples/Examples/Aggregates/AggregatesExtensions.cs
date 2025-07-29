using Examples.Aggregates.Applier;
using Examples.Aggregates.Commander;
using Examples.Aggregates.Storage;
using MinimalKafka;
using MinimalKafka.Stream;

namespace Examples.Aggregates;

public static class AggregatesExtensions
{
    public static IServiceCollection InMemoryAggregateStore(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IAggregateStore<,>), typeof(InMemoryStateStore<,>));
        return services;
    }

    public static IKafkaConventionBuilder MapApplier<TKey, TEvent, TState>(
        this IApplicationBuilder appBuilder, string eventTopic)
        where TKey : notnull
        where TState : IAggregate<TKey>, IEventApplier<TKey, TState, TEvent>, new()
        where TEvent : IEvent<TKey>
    {
        return appBuilder.MapApplier<TState, TKey, TState, TEvent>(eventTopic);
    }

    public static IKafkaConventionBuilder MapApplier<TApplier, TKey, TState, TEvent>(
        this IApplicationBuilder appBuilder, string eventTopic)
        where TKey : notnull
        where TApplier : IEventApplier<TKey, TState, TEvent>
        where TState : IAggregate<TKey>, new()
        where TEvent : IEvent<TKey>
    {
        var builder = new ApplierBuilder<TKey, TState, TEvent>();
        
        TApplier.Configure(builder);
        
        var func = builder.Build();
        
        return appBuilder
            .MapStream<TKey, TEvent>(eventTopic)
            .Into(func);
    }

    public static IKafkaConventionBuilder MapCommander<TKey, TState, TCommand, TEvent>(
        this IApplicationBuilder appBuilder, string commandTopic, string eventTopic)
        where TKey : notnull
        where TState : IAggregate<TKey>, ICommander<TKey, TState, TCommand, TEvent>, new()
        where TCommand : ICommand<TKey>
        where TEvent : IEvent<TKey>
    {
        return appBuilder.MapCommander<TState, TKey, TState, TCommand, TEvent>(commandTopic, eventTopic);
    }

    public static IKafkaConventionBuilder MapCommander<TCommander, TKey, TState, TCommand, TEvent>(
        this IApplicationBuilder appBuilder, string commandTopic, string eventTopic)
        where TKey : notnull
        where TCommander : ICommander<TKey, TState, TCommand, TEvent>
        where TState : IAggregate<TKey>, new()
        where TCommand : ICommand<TKey>
        where TEvent : IEvent<TKey>
    {
        var builder = new CommanderBuilder<TKey, TState, TCommand, TEvent>(eventTopic);

        TCommander.Configure(builder);

        var func = builder.Build();
        
        return appBuilder
            .MapStream<TKey, TCommand>(commandTopic)
            .Into(func);
    }
}

