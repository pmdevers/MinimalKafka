using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;
using System.Diagnostics;

namespace MinimalKafka.Middlewares;

/// <summary>
/// Descibes the middleware.
/// </summary>
public delegate Task KafkaMiddlewareDelegate(KafkaContext context, KafkaDelegate next);

/// <summary>
/// Base class to implment a KafkaMiddleware.
/// </summary>
public interface IKafkaMiddleware
{
    /// <summary>
    /// Invoke the middleware with the given context.
    /// </summary>
    Task InvokeAsync(KafkaContext context, KafkaDelegate next);
}

internal sealed class LoggerMiddleware(ILogger<LoggerMiddleware> logger) : IKafkaMiddleware
{
    public async Task InvokeAsync(KafkaContext context, KafkaDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.StartConsume(context.TopicName, context.GroupId);

        await next(context);

        logger.FinishedConsume(context.TopicName, context.GroupId, stopwatch.Elapsed);
    }
}

/// <summary>
/// KafkaMiddleware Extensions for KafkaConvention Builder.
/// </summary>
public static class KafkaBuilderMiddlewareExtensions
{
    /// <summary>
    /// Adds a KafkaMiddleware to the pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the middleware.</typeparam>
    public static IKafkaConventionBuilder Use<T>(this IKafkaConventionBuilder builder)
        where T : IKafkaMiddleware
    {
        builder.Add(x =>
        {
            x.Middlewares.Add((s) => ActivatorUtilities.CreateInstance<T>(s).InvokeAsync);
        });
        return builder;
    }

    /// <summary>
    /// Adds a KafkaMiddleware to the pipeline.
    /// </summary>
    public static void Use(this IKafkaConventionBuilder builder, Func<KafkaContext, KafkaDelegate, Task> middleware)
    {
        builder.Add(x =>
        {
            x.Middlewares.Add((_) => (ctx, n) => middleware(ctx, n));
        });
    }

    /// <summary>
    /// Adds a KafkaMiddleware to the pipeline.
    /// </summary>
    public static void Use(this IKafkaConventionBuilder builder, KafkaMiddlewareDelegate middleware)
    {
        builder.Add(x =>
        {
            x.Middlewares.Add((_) => middleware);
        });
    }

    /// <summary>
    /// Adds extended logging middleware to the Kafka convention builder pipeline.
    /// </summary>
    public static IKafkaConventionBuilder WithExtendedLogging(this IKafkaConventionBuilder builder)
    {
        return builder.Use<LoggerMiddleware>();
    }
}
