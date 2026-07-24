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
    string TopicName { get; }

    /// <summary>
    /// 
    /// </summary>
    void Subscribe();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task<KafkaContext> Consume(CancellationToken cancellationToken);

    /// <summary>
    /// Commits the processed message offset.
    /// </summary>
    /// <param name="context">The consumed context to commit.</param>
    void Commit(KafkaContext context);

    /// <summary>
    /// 
    /// </summary>
    void Close();
}

/// <summary>
/// Delegate for handling kafka messages.
/// </summary>
public delegate Task KafkaDelegate(KafkaContext context);