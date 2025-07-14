namespace MinimalKafka.Stream;

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
        where TKey : notnull
        => builder.On((k, _) => k, (k, _) => k);
}