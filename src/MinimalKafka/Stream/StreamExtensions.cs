using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Stream.Internals;

namespace MinimalKafka.Stream;

/// <summary>
/// Provides extension methods for mapping Kafka topics to stream builders in MinimalKafka.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Maps a Kafka topic to a stream builder using the application's <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the message key.</typeparam>
    /// <typeparam name="TValue">The type of the message value.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <param name="topic">The name of the Kafka topic.</param>
    /// <returns>An <see cref="IStreamBuilder{TKey, TValue}"/> for further stream configuration.</returns>
    public static IStreamBuilder<TKey, TValue> MapStream<TKey, TValue>(this IApplicationBuilder builder, string topic)
        where TKey : notnull
    {
        var sb = builder.ApplicationServices.GetRequiredService<IKafkaBuilder>();

        return sb.MapStream<TKey, TValue>(topic);
    }

    /// <summary>
    /// Maps a Kafka topic to a stream builder using the specified <see cref="IKafkaBuilder"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the message key.</typeparam>
    /// <typeparam name="TValue">The type of the message value.</typeparam>
    /// <param name="builder">The Kafka builder.</param>
    /// <param name="topic">The name of the Kafka topic.</param>
    /// <returns>An <see cref="IStreamBuilder{TKey, TValue}"/> for further stream configuration.</returns>
    public static IStreamBuilder<TKey, TValue> MapStream<TKey, TValue>(this IKafkaBuilder builder, string topic)
        where TKey : notnull
    {
        var sb = new StreamBuilder<TKey, TValue>(builder, topic);
        return sb;
    }
}