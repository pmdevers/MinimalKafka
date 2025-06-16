using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Metadata.Internals;

namespace MinimalKafka.Extension;

/// <summary>
/// Provides extension methods for configuring key and value deserializers on Kafka consumer builders.
/// </summary>
public static class KafkaConsumerBuilderMetadataExtensions
{
    /// <summary>
    /// Registers a key deserializer using a factory delegate for the consumer builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">A factory delegate that returns a key deserializer instance.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithKeyDeserializer<TBuilder>(this TBuilder builder, Func<IKafkaConsumerBuilder, object> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new KeyDeserializerMetadata(serializer));
        return builder;
    }

    /// <summary>
    /// Registers a key deserializer by type for the consumer builder.
    /// The type must implement <c>IDeserializer&lt;&gt;</c>.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The deserializer type.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type does not implement <c>IDeserializer&lt;&gt;</c>.</exception>
    public static TBuilder WithKeyDeserializer<TBuilder>(this TBuilder builder, Type serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        if (!serializer.IsTheGenericType(typeof(IDeserializer<>)))
        {
            throw new InvalidOperationException($"Type '{serializer}' should of type '{typeof(IDeserializer<>)}'");
        }
        builder.WithKeyDeserializer((s) => s.ServiceProvider.GetRequiredService(serializer.MakeGenericType(s.KeyType)));
        return builder;
    }

    /// <summary>
    /// Registers a specific key deserializer instance for the consumer builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <typeparam name="T">The key type.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The key deserializer instance.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithKeyDeserializer<TBuilder, T>(this TBuilder builder, IDeserializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithKeyDeserializer((s) => serializer);
        return builder;
    }

    /// <summary>
    /// Registers a value deserializer using a factory delegate for the consumer builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">A factory delegate that returns a value deserializer instance.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithValueDeserializer<TBuilder>(this TBuilder builder, Func<IKafkaConsumerBuilder, object> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new ValueDeserializerMetadata(serializer));
        return builder;
    }

    /// <summary>
    /// Registers a value deserializer by type for the consumer builder.
    /// The type must implement <c>IDeserializer&lt;&gt;</c>.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The deserializer type.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type does not implement <c>IDeserializer&lt;&gt;</c>.</exception>
    public static TBuilder WithValueDeserializer<TBuilder>(this TBuilder builder, Type serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        if (!serializer.IsTheGenericType(typeof(IDeserializer<>)))
        {
            throw new InvalidOperationException($"Type '{serializer}' should of type '{typeof(IDeserializer<>)}'");
        }
        builder.WithValueDeserializer((s) => s.ServiceProvider.GetRequiredService(serializer.MakeGenericType(s.ValueType)));
        return builder;
    }

    /// <summary>
    /// Registers a specific value deserializer instance for the consumer builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The value deserializer instance.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithValueDeserializer<TBuilder, T>(this TBuilder builder, IDeserializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithValueDeserializer((s) => serializer);
        return builder;
    }

    /// <summary>
    /// Determines whether the specified type is or implements the given generic type definition.
    /// </summary>
    /// <param name="candidateType">The type to check.</param>
    /// <param name="genericType">The generic type definition to compare against.</param>
    /// <returns><c>true</c> if the type matches or implements the generic type; otherwise, <c>false</c>.</returns>
    private static bool IsTheGenericType(this Type candidateType, Type genericType)
    {
        return
            candidateType != null && genericType != null &&
            (candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == genericType ||
             candidateType.GetInterfaces().ToList().Exists(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType) ||
             candidateType.BaseType != null && candidateType.BaseType.IsTheGenericType(genericType));
    }
}
