using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Stream;
using MinimalKafka.Stream.Storage;

namespace MinimalKafka.Extension;

/// <summary>
/// Provides extension methods for <see cref="IAddKafkaBuilder"/> to configure stream stores.
/// </summary>
public static class AddKafkaBuilderExtensions
{
    /// <summary>
    /// Registers the in-memory stream store implementation for use with MinimalKafka.
    /// </summary>
    /// <param name="builder">The Kafka builder to configure.</param>
    /// <returns>The same <see cref="IAddKafkaBuilder"/> instance for chaining.</returns>
    public static IAddKafkaBuilder WithInMemoryStore(this IAddKafkaBuilder builder)
    {
        return builder.WithStreamStoreFactory(new InMemoryStreamStoreFactory());
    }

    /// <summary>
    /// Registers a custom stream store implementation for use with MinimalKafka.
    /// </summary>
    /// <param name="builder">The Kafka builder to configure.</param>
    /// <param name="streamStoreFactory">The type of the stream store to register. Must implement <c>IStreamStore&lt;,&gt;</c>.</param>
    /// <returns>The same <see cref="IAddKafkaBuilder"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="streamStoreFactory"/> does not implement <c>IStreamStore&lt;,&gt;</c>.</exception>
    public static IAddKafkaBuilder WithStreamStoreFactory(this IAddKafkaBuilder builder, IStreamStoreFactory streamStoreFactory)
    {
        builder.Services.AddSingleton(streamStoreFactory);

        return builder;
    }
}
