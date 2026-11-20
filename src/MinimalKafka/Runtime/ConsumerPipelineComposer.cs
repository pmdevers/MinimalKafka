using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Middleware;

namespace MinimalKafka.Runtime;

/// <summary>
/// Composes consumer delegate pipelines by combining a handler with configured middleware components.
/// </summary>
/// <remarks>Builds a single executable delegate from middleware resolved from dependency injection and
/// registration-provided middleware factories, applying middleware in reverse order so each component wraps the next
/// handler in the pipeline.</remarks>
public static class ConsumerPipelineComposer
{
    /// <summary>
    /// Composes a consumer delegate for a topic by combining the registered handler with middleware resolved from the
    /// service provider and middleware defined by the topic registration.
    /// </summary>
    /// <remarks>Middleware resolved from the service provider is applied before middleware created from the
    /// topic registration.</remarks>
    /// <param name="registration">The topic registration that provides the handler and registration-specific middleware factories.</param>
    /// <param name="services">The service provider used to resolve consumer middleware and create registration-specific middleware instances.</param>
    /// <returns>A consumer delegate that executes the composed middleware pipeline and invokes the registered handler.</returns>
    public static ConsumerDelegate Compose(TopicRegistration registration, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(services);

        var middleware = services.GetServices<IConsumerMiddleware>()
            .Concat(registration.Middleware.Select(create => create(services)));

        return Compose(registration.Handler, middleware);
    }

    /// <summary>
    /// Builds a consumer delegate pipeline by wrapping the handler with the provided middleware in registration order.
    /// </summary>
    /// <remarks>Middleware is applied by iterating the sequence in reverse so the first middleware in the
    /// input executes first.</remarks>
    /// <param name="handler">The terminal delegate executed after all middleware has run.</param>
    /// <param name="middleware">The middleware components to apply around the handler.</param>
    /// <returns>A composed consumer delegate that executes each middleware and then the handler.</returns>
    public static ConsumerDelegate Compose(ConsumerDelegate handler, IEnumerable<IConsumerMiddleware> middleware)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(middleware);

        var pipeline = handler;
        foreach (var current in middleware.Reverse())
        {
            var next = pipeline;
            pipeline = context => current.InvokeAsync(context, next);
        }

        return pipeline;
    }
}
