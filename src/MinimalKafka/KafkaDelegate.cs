namespace MinimalKafka;

/// <summary>
/// Represents a delegate that handles a Kafka message within a given <see cref="KafkaContext"/>.
/// </summary>
/// <param name="context">The context for the Kafka message, providing access to the message key, value, headers, metadata, and services.</param>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task KafkaDelegate(KafkaContext context);