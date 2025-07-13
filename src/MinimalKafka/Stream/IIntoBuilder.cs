namespace MinimalKafka.Stream;

/// <summary>
/// Defines a builder interface for configuring a handler that processes Kafka messages with a key and value type.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TValue">The type of the message value.</typeparam>
public interface IIntoBuilder<TKey, TValue>
{
    /// <summary>
    /// Registers a handler to process Kafka messages with the specified key and value types.
    /// </summary>
    /// <param name="handler">A function that processes the Kafka context, message key, and message value.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> for further configuration.</returns>
    IKafkaConventionBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler);
}

/// <summary>
/// Defines a builder interface for configuring a handler that processes Kafka messages with a single value type.
/// </summary>
/// <typeparam name="TValue">The type of the message value.</typeparam>
public interface IIntoBuilder<TValue>
{
    /// <summary>
    /// Registers a handler to process Kafka messages with the specified value type.
    /// </summary>
    /// <param name="handler">A function that processes the Kafka context and message value.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> for further configuration.</returns>
    IKafkaConventionBuilder Into(Func<KafkaContext, TValue, Task> handler);
}