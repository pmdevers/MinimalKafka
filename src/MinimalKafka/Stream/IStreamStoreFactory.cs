namespace MinimalKafka.Stream;

/// <summary>
/// Describes a StreamStore Factory
/// </summary>
public interface IStreamStoreFactory
{
    /// <summary>
    /// Creates a new StreamStore from the Factory
    /// </summary>
    /// <typeparam name="TKey">The Key Type of the Factory</typeparam>
    /// <typeparam name="TValue">The Value Type of th Factory</typeparam>
    /// <returns>A Stream Store</returns>
    IStreamStore<TKey, TValue> GetStreamStore<TKey, TValue>()
        where TKey : notnull;
}
