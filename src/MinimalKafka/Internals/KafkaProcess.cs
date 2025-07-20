using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;

namespace MinimalKafka.Internals;

internal class KafkaProcess(
    IKafkaConsumer consumer, 
    ILogger<KafkaProcess> logger) : IKafkaProcess
{
    public static KafkaProcess Create(IKafkaConsumer consumer, ILogger<KafkaProcess> logger)
        => new(consumer, logger);

    public async Task Start(CancellationToken token)
    {
        consumer.Subscribe();

        try
        {
            
            await consumer.Consume(token);
        } 
        catch (Exception ex) 
        {
            logger.UnknownProcessException(consumer.TopicName, ex.Message);
            throw new KafkaProcesException(ex, "Unknown Process error.");
        }
        finally
        {
            consumer.Close();
            logger.DropOutOfConsumeLoop();
        }
    }

    public Task Stop()
    {
        consumer.Close();
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