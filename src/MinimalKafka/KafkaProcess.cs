using MinimalKafka.Helpers;

namespace MinimalKafka;

/// <summary>
/// Represents a Kafka process that can be started and stopped.
/// </summary>
public interface IKafkaProcess
{
    /// <summary>
    /// Starts the Kafka process.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous start operation.</returns>
    Task Start(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the Kafka process.
    /// </summary>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    Task Stop();
}

/// <summary>
/// Represents the configuration options for processing Kafka messages.
/// </summary>
/// <remarks>This class provides options to configure the Kafka consumer and the delegate that defines the
/// behavior for processing Kafka message contexts. Use these options to customize how messages are consumed and
/// handled.</remarks>
public class KafkaProcessOptions
{
    /// <summary>
    /// Gets or sets the Kafka consumer instance used to consume messages from a Kafka topic.
    /// </summary>
    public KafkaConsumer Consumer { get; set; } = new NoConsumer();

    /// <summary>
    /// Gets or sets the delegate that defines the behavior to execute for a Kafka message context.
    /// </summary>
    /// <remarks>Assign a custom delegate to specify the logic to handle Kafka message contexts.  The delegate
    /// should return a <see cref="Task"/> to support asynchronous execution.</remarks>
    public KafkaDelegate Delegate { get; set; } = (context) => Task.CompletedTask;
}
internal sealed class KafkaProcess : IKafkaProcess
{
    private readonly KafkaConsumer _consumer;
    private readonly KafkaDelegate _handler;

    private KafkaProcess(
        KafkaConsumer consumer,
        KafkaDelegate handler
    )
    {
        _consumer = consumer;
        _handler = handler;
    }

    public static KafkaProcess Create(KafkaProcessOptions options)
        => new(options.Consumer, options.Delegate);

    public async Task Start(CancellationToken cancellationToken)
    {
        _consumer.Subscribe();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _consumer.Consume(_handler, cancellationToken);
            }
        }
        catch(Exception ex)
        {
            _consumer.Logger.UnknownProcessException(ex.Message);
            throw new KafkaProcesException(ex, "Unknown Process error.");
        }
        finally
        {
            _consumer.Logger.DropOutOfConsumeLoop();
        }
        
    }

    public async Task Stop()
    {
        _consumer.Close();
        await Task.CompletedTask;
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