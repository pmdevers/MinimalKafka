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
    /// 
    /// </summary>
    /// <param name="topicName"></param>
    /// <returns></returns>
    IBranchBuilder<TKey, TValue> To(string topicName);
}
