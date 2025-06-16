namespace MinimalKafka.Stream;

/// <summary>
/// Defines a builder interface for constructing stream processing topologies with support for joining streams.
/// </summary>
/// <typeparam name="K1">The type of the key in the primary stream.</typeparam>
/// <typeparam name="V1">The type of the value in the primary stream.</typeparam>
public interface IStreamBuilder<K1, V1> : IIntoBuilder<K1, V1>
{
    /// <summary>
    /// Creates a join operation between the current stream and another stream specified by the given topic.
    /// </summary>
    /// <remarks>The join operation combines records from the current stream with records from the specified
    /// stream based on matching keys. The resulting <see cref="IJoinBuilder{K1, V1, K2, V2}"/> can be used to configure
    /// additional join parameters, such as the join window or result processing.</remarks>
    /// <typeparam name="K2">The type of the key in the other stream.</typeparam>
    /// <typeparam name="V2">The type of the value in the other stream.</typeparam>
    /// <param name="topic">The name of the topic representing the other stream to join with. Cannot be null or empty.</param>
    /// <returns>An <see cref="IJoinBuilder{K1, V1, K2, V2}"/> that allows further configuration of the join operation.</returns>
    IJoinBuilder<K1, V1, K2, V2> Join<K2, V2>(string topic);
}

