namespace MinimalKafka.Stream;

/// <summary>
/// Defines a builder for configuring a branch in a stream processing topology.
/// </summary>
/// <remarks>This interface is used to specify the target topic for a branch in the stream processing
/// topology.</remarks>
/// <typeparam name="TKey">The type of the key in the stream.</typeparam>
/// <typeparam name="TValue">The type of the value in the stream.</typeparam>
public interface IToBranchBuilder<TKey, TValue>
{
    /// <summary>
    /// Specifies the target topic for this branch in the stream processing topology.
    /// </summary>
    /// <param name="topicName">The name of the Kafka topic where matching records will be routed.</param>
    /// <returns>An <see cref="IBranchBuilder{TKey, TValue}"/> to continue building the stream topology.</returns>
    IBranchBuilder<TKey, TValue> To(string topicName);
}
