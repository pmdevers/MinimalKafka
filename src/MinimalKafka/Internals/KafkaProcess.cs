using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;
using MinimalKafka.Middlewares;

namespace MinimalKafka.Internals;

internal sealed class KafkaProcess(
    IKafkaConsumerBuilder consumerBuilder,
    IKafkaProducer producer,
    IReadOnlyList<Func<IServiceProvider, KafkaMiddlewareDelegate>> middlewares,
    ILogger<KafkaProcess> logger) : IKafkaProcess
{
    private readonly IKafkaConsumer _consumer = consumerBuilder.Build();

    public async Task Start(CancellationToken token)
    {
        _consumer.Subscribe();

        try
        {
            while (!token.IsCancellationRequested)
            {
                using var context = await _consumer.Consume(token);

                if (context is EmptyKafkaContext)
                {
                    logger.EmptyContext();
                    continue;
                }

                await Invoke(context);

                await producer.ProduceAsync(context, token);
            }
        }
        catch (Exception ex)
        {
            logger.UnknownProcessException(_consumer.TopicName, ex.Message);
            throw new KafkaProcesException(ex, $"Unknown Process error while handling topic '{_consumer.TopicName}'");
        }
        finally
        {
            _consumer.Close();
            logger.DropOutOfConsumeLoop();
        }
    }

    public async Task Invoke(KafkaContext context)
    {
        KafkaDelegate next = (_) => Task.CompletedTask;

        for (int i = middlewares.Count - 1; i >= 0; i--)
        {
            var currentMiddleware = middlewares[i].Invoke(context.RequestServices);
            KafkaDelegate prevNext = next;
            next = (context) => currentMiddleware(context, prevNext);
        }

        await next(context);
    }

    public Task Stop()
    {
        _consumer.Close();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents an exception that occurs during the processing of Kafka messages.
/// </summary>
/// <remarks>This exception is typically thrown when an error occurs while handling Kafka messages, such as
/// deserialization issues, message processing failures, or other unexpected conditions during Kafka consumer or
/// producer operations.</remarks>
/// <param name="ex">The inner exception that caused this exception to be thrown.</param>
/// <param name="message">A message that describes the error.</param>
public class KafkaProcesException(Exception ex, string message) : Exception(message, ex)
{
}