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
    IJoinBuilder<K1, V1, K2, V2> Join<K2, V2>(string topic)
        where K2 : notnull;

    /// <summary>
    /// Creates an inner-join operation between the current stream and another stream specified by the given topic.
    /// </summary>
    IJoinBuilder<K1, V1, K2, V2> InnerJoin<K2, V2>(string topic)
        where K2 : notnull;
}
