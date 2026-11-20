using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Producing;

/// <summary>
/// Composes producer delegate pipelines by combining a publish delegate with configured middleware components.
/// </summary>
/// <remarks>Builds a single executable delegate from middleware resolved from dependency injection,
/// applying middleware in reverse order so each component wraps the next delegate in the pipeline.</remarks>
public static class ProducerPipelineComposer
{
    /// <summary>
    /// Composes a producer delegate by combining middleware resolved from the service provider with
    /// the terminal publish delegate.
    /// </summary>
    /// <param name="handler">The terminal delegate executed after all middleware has run.</param>
    /// <param name="services">The service provider used to resolve producer middleware components.</param>
    /// <returns>A composed producer delegate that executes each middleware and then the publish delegate.</returns>
    public static ProducerDelegate Compose(ProducerDelegate handler, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(services);

        var middleware = services.GetServices<IProducerMiddleware>();
        return Compose(handler, middleware);
    }

    /// <summary>
    /// Builds a producer delegate pipeline by wrapping the handler with the provided middleware in registration order.
    /// </summary>
    /// <remarks>Middleware is applied by iterating the sequence in reverse so the first middleware in the
    /// input executes first.</remarks>
    /// <param name="handler">The terminal delegate executed after all middleware has run.</param>
    /// <param name="middleware">The middleware components to apply around the handler.</param>
    /// <returns>A composed producer delegate that executes each middleware and then the handler.</returns>
    public static ProducerDelegate Compose(ProducerDelegate handler, IEnumerable<IProducerMiddleware> middleware)
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
