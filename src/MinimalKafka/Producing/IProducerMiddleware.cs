namespace MinimalKafka.Producing;

/// <summary>Represents middleware that can inspect or modify produced Kafka messages.</summary>
public interface IProducerMiddleware
{
    /// <summary>Invokes the middleware for the current produce operation.</summary>
    /// <param name="context">The current producer context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that completes when processing finishes.</returns>
    Task InvokeAsync(ProducerContext context, ProducerDelegate next);
}

