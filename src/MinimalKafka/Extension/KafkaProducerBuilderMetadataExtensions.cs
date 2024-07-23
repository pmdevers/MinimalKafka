using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;

namespace MinimalKafka.Extension;

public static class KafkaProducerBuilderMetadataExtensions
{
    public static TBuilder WithKeySerializer<TBuilder>(this TBuilder builder, Func<IKafkaProducerBuilder, object> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new KeySerializerMetadata(serializer));
        return builder;
    }

    public static TBuilder WithKeySerializer<TBuilder>(this TBuilder builder, Type serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        if (!serializer.IsTheGenericType(typeof(ISerializer<>)))
        {
            throw new InvalidOperationException($"Type '{serializer}' should of type '{typeof(ISerializer<>)}'");
        }
        builder.WithKeySerializer((s) => s.ServiceProvider.GetRequiredService(serializer.MakeGenericType(s.KeyType)));
        return builder;
    }

    public static TBuilder WithKeySerializer<TBuilder, T>(this TBuilder builder, IDeserializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithKeySerializer((s) => serializer);
        return builder;
    }

    public static TBuilder WithValueSerializer<TBuilder>(this TBuilder builder, Func<IKafkaProducerBuilder, object> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new ValueSerializerMetadata(serializer));
        return builder;
    }

    public static TBuilder WithValueSerializer<TBuilder>(this TBuilder builder, Type serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        if (!serializer.IsTheGenericType(typeof(ISerializer<>)))
        {
            throw new InvalidOperationException($"Type '{serializer}' should of type '{typeof(ISerializer<>)}'");
        }
        builder.WithValueSerializer((s) => s.ServiceProvider.GetRequiredService(serializer.MakeGenericType(s.ValueType)));
        return builder;
    }

    public static TBuilder WithValueSerializer<TBuilder, T>(this TBuilder builder, ISerializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithValueSerializer((s) => serializer);
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