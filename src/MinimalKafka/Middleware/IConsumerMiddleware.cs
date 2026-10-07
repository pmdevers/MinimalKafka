namespace MinimalKafka.Middleware;

public interface IConsumerMiddleware
{
    Task InvokeAsync(KafkaContext context, ConsumerDelegate next);
}
