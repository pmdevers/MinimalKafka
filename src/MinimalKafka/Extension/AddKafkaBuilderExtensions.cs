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
        return builder.WithStreamStore(typeof(InMemoryStore<,>));
    }

    /// <summary>
    /// Registers a custom stream store implementation for use with MinimalKafka.
    /// </summary>
    /// <param name="builder">The Kafka builder to configure.</param>
    /// <param name="streamStoreType">The type of the stream store to register. Must implement <c>IStreamStore&lt;,&gt;</c>.</param>
    /// <returns>The same <see cref="IAddKafkaBuilder"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="streamStoreType"/> does not implement <c>IStreamStore&lt;,&gt;</c>.</exception>
    public static IAddKafkaBuilder WithStreamStore(this IAddKafkaBuilder builder, Type streamStoreType)
    {
        if (!Array.Exists(streamStoreType.GetInterfaces(),
            x => x.IsGenericType &&
                 x.GetGenericTypeDefinition() == typeof(IStreamStore<,>)
        ))
        {
            throw new InvalidOperationException($"Type: '{streamStoreType}' does not implement IStreamStore<,>");
        }

        builder.Services.AddSingleton(typeof(IStreamStore<,>), streamStoreType);

        return builder;
    }
}
