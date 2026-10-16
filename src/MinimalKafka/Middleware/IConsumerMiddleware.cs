namespace MinimalKafka.Middleware;

/// <summary>Represents middleware that can inspect or modify consumed Kafka messages.</summary>
public interface IConsumerMiddleware
{
    /// <summary>Invokes the middleware for the current consumed message.</summary>
    /// <param name="context">The current consumer context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that completes when processing finishes.</returns>
    Task InvokeAsync(KafkaContext context, ConsumerDelegate next);
}
