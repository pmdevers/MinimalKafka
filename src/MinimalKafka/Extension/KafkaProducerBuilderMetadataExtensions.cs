using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;

namespace MinimalKafka.Extension;

public static class KafkaProducerBuilderMetadataExtensions
{
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

    public static TBuilder WithKeySerializer<TBuilder, T>(this TBuilder builder, ISerializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithKeySerializer(serializer.GetType());
        return builder;
    }

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

    public static TBuilder WithValueSerializer<TBuilder, T>(this TBuilder builder, ISerializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithValueSerializer(serializer.GetType());
        return builder;
    }

    private static bool IsTheGenericType(this Type candidateType, Type genericType)
    {
        return
            candidateType != null && genericType != null &&
            (candidateType.IsGenericType && candidateType.GetGenericTypeDefinition() == genericType ||
             candidateType.GetInterfaces().ToList().Exists(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType) ||
             candidateType.BaseType != null && candidateType.BaseType.IsTheGenericType(genericType));
    }
}