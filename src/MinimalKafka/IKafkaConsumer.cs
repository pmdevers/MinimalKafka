namespace MinimalKafka;

/// <summary>
/// Defines the contract for a Kafka consumer that can subscribe to topics, consume messages, and close the connection.
/// </summary>
/// <remarks>Implementations of this interface are responsible for interacting with Kafka to retrieve messages
/// from subscribed topics. Ensure proper resource management by calling <see cref="Close"/> when the consumer is no
/// longer needed.</remarks>
public interface IKafkaConsumer
{
    /// <summary>
    /// 
    /// </summary>
    void Subscribe();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task Consume(CancellationToken cancellationToken);
    /// <summary>
    /// 
    /// </summary>
    void Close();
}
