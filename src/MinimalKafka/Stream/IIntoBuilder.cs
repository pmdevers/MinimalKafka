using MinimalKafka.Builders;
using MinimalKafka.Extension;

namespace MinimalKafka.Stream;

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
/// Class with extension methods for <see cref="IBranchBuilder{TKey, TValue}"/>.
/// </summary>
public static class IIntoBuilderExtensions 
{
    /// <summary>
    /// Configures the specified builder to produce messages to the given Kafka topic.
    /// </summary>
    /// <typeparam name="TKey">The type of the message key.</typeparam>
    /// <typeparam name="V1">The type of the first value in the message payload.</typeparam>
    /// <typeparam name="V2">The type of the second value in the message payload.</typeparam>
    /// <param name="builder">The builder used to configure the Kafka message production.</param>
    /// <param name="topic">The name of the Kafka topic to which messages will be produced. Cannot be null or empty.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> instance configured to produce messages to the specified topic.</returns>
    public static IKafkaConventionBuilder Into<TKey, V1, V2>(this IIntoBuilder<TKey, (V1?, V2?)> builder, string topic)
        => builder.Into(async (c, k, v) =>
        {
            await c.ProduceAsync(topic, k, v);
        });
}