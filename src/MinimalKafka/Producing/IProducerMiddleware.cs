namespace MinimalKafka.Producing;

public interface IProducerMiddleware
{
    Task InvokeAsync(ProducerContext context, ProducerDelegate next);
}




