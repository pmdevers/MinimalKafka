using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Aggregates.Applier;
using MinimalKafka.Aggregates.Commander;
using MinimalKafka.Aggregates.Storage;
using MinimalKafka.Stream;

namespace MinimalKafka.Aggregates;

/// <summary>
/// this class contains extension methods for registering and mapping aggregates in a Kafka-based application.
/// </summary>
public static class AggregateExtensions
{
    /// <summary>
    /// this method registers the in-memory aggregate store as the default aggregate store.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection InMemoryAggregateStore(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IAggregateStore<,>), typeof(InMemoryStateStore<,>));
        return services;
    }

    /// <summary>
    /// this method maps an event topic to an applier for a specific aggregate type.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <param name="appBuilder"></param>
    /// <param name="eventTopic"></param>
    /// <returns></returns>
    public static IKafkaConventionBuilder MapApplier<TKey, TEvent, TState>(
        this IApplicationBuilder appBuilder, string eventTopic)
        where TKey : notnull
        where TState : IAggregate<TKey>, IEventApplier<TKey, TState, TEvent>, new()
        where TEvent : IEvent<TKey>
    {
        return appBuilder.MapApplier<TState, TKey, TState, TEvent>(eventTopic);
    }

    /// <summary>
    /// this method maps an event topic to an applier for a specific aggregate type.
    /// </summary>
    /// <typeparam name="TApplier"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="appBuilder"></param>
    /// <param name="eventTopic"></param>
    /// <returns></returns>
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

    /// <summary>
    /// this method maps a command topic to a commander for a specific aggregate type.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="appBuilder"></param>
    /// <param name="commandTopic"></param>
    /// <param name="eventTopic"></param>
    /// <returns></returns>
    public static IKafkaConventionBuilder MapCommander<TKey, TState, TCommand, TEvent>(
        this IApplicationBuilder appBuilder, string commandTopic, string eventTopic)
        where TKey : notnull
        where TState : IAggregate<TKey>, ICommander<TKey, TState, TCommand, TEvent>, new()
        where TCommand : ICommand<TKey>
        where TEvent : IEvent<TKey>
    {
        return appBuilder.MapCommander<TState, TKey, TState, TCommand, TEvent>(commandTopic, eventTopic);
    }

    /// <summary>
    /// this method maps a command topic to a commander for a specific aggregate type.
    /// </summary>
    /// <typeparam name="TCommander"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TState"></typeparam>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="appBuilder"></param>
    /// <param name="commandTopic"></param>
    /// <param name="eventTopic"></param>
    /// <returns></returns>
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

