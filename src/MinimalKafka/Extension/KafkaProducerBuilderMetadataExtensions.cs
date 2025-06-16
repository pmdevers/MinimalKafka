using Confluent.Kafka;
using MinimalKafka.Builders;
using MinimalKafka.Metadata.Internals;

namespace MinimalKafka.Extension;

/// <summary>
/// Provides extension methods for configuring key and value serializers on Kafka producer builders.
/// </summary>
public static class KafkaProducerBuilderMetadataExtensions
{
    /// <summary>
    /// Registers a key serializer by type for the producer builder.
    /// The type must implement <c>ISerializer&lt;&gt;</c>.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The serializer type.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type does not implement <c>ISerializer&lt;&gt;</c>.</exception>
    public static TBuilder WithKeySerializer<TBuilder>(this TBuilder builder, Type serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        if (!serializer.IsTheGenericType(typeof(ISerializer<>)))
        {
            throw new InvalidOperationException($"Type '{serializer}' should of type '{typeof(ISerializer<>)}'");
        }
        builder.WithSingle(new KeySerializerMetadata(serializer));
        return builder;
    }

    /// <summary>
    /// Registers a specific key serializer instance for the producer builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <typeparam name="T">The key type.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The key serializer instance.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithKeySerializer<TBuilder, T>(this TBuilder builder, ISerializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithKeySerializer(serializer.GetType());
        return builder;
    }

    /// <summary>
    /// Registers a value serializer by type for the producer builder.
    /// The type must implement <c>ISerializer&lt;&gt;</c>.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The serializer type.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type does not implement <c>ISerializer&lt;&gt;</c>.</exception>
    public static TBuilder WithValueSerializer<TBuilder>(this TBuilder builder, Type serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        if (!serializer.IsTheGenericType(typeof(ISerializer<>)))
        {
            throw new InvalidOperationException($"Type '{serializer}' should of type '{typeof(ISerializer<>)}'");
        }
        builder.WithSingle(new ValueSerializerMetadata(serializer));
        return builder;
    }

    /// <summary>
    /// Registers a specific value serializer instance for the producer builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka convention builder.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="serializer">The value serializer instance.</param>
    /// <returns>The same <typeparamref name="TBuilder"/> instance for chaining.</returns>
    public static TBuilder WithValueSerializer<TBuilder, T>(this TBuilder builder, ISerializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithValueSerializer(serializer.GetType());
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