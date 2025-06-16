namespace MinimalKafka.Stream;

/// <summary>
/// Defines a builder interface for configuring join operations between two Kafka streams.
/// </summary>
/// <typeparam name="K1">The type of the key in the left stream.</typeparam>
/// <typeparam name="V1">The type of the value in the left stream.</typeparam>
/// <typeparam name="K2">The type of the key in the right stream.</typeparam>
/// <typeparam name="V2">The type of the value in the right stream.</typeparam>
public interface IJoinBuilder<K1, V1, K2, V2>
{
    /// <summary>
    /// Configures a join operation using a predicate to match values from the left and right streams.
    /// </summary>
    /// <param name="on">A function that determines whether two values (from the left and right streams) should be joined.</param>
    /// <returns>An <see cref="IIntoBuilder{TValue}"/> for further configuration of the joined stream.</returns>
    IIntoBuilder<(V1, V2)> On(Func<V1, V2, bool> on);

    /// <summary>
    /// Configures a join operation using key selectors for the left and right streams.
    /// </summary>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <param name="leftKey">A function to extract the join key from the left stream's key and value.</param>
    /// <param name="rightKey">A function to extract the join key from the right stream's key and value.</param>
    /// <returns>An <see cref="IIntoBuilder{TKey, TValue}"/> for further configuration of the joined stream.</returns>
    IIntoBuilder<TKey, (V1?, V2?)> On<TKey>(Func<K1, V1, TKey> leftKey, Func<K2, V2, TKey> rightKey);
}


/// <summary>
/// Class with extension methods for <see cref="IJoinBuilder{TKey, V1, TKey, V2}"/>.
/// </summary>
public static class IJoinBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="V1"></typeparam>
    /// <typeparam name="V2"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IIntoBuilder<TKey, (V1?, V2?)> OnKey<TKey, V1, V2>(this IJoinBuilder<TKey, V1, TKey, V2> builder)
        => builder.On((k, _) => k, (k, _) => k);
}
