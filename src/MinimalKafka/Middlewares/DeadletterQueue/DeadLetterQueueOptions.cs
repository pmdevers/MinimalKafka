namespace MinimalKafka.Middlewares.DeadletterQueue;

/// <summary>
/// Represents the configuration options for the DeadLetterQueueMiddleware.
/// </summary>
public class DeadLetterQueueOptions
{
    /// <summary>
    /// Gets or sets the name of the dead letter queue topic.
    /// </summary>
    public string Topic { get; set; } = "dead-letter-queue";

    /// <summary>
    /// Gets or sets a value indicating whether to automatically resolve messages in the dead letter queue.
    /// </summary>
    public bool ResolveAutomatically { get; set; } = true;
}
