using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;
using TopicMetadata = MinimalKafka.Metadata.TopicMetadata;

namespace MinimalKafka.Extension;
public static class KafkaConsumerBuilderMetadataExtensions
{
    private static readonly Func<Type, string> DefaultNamingConvention = type => type
        .FQN()
        .Replace('<', '-')
        .Replace('<', '-')
        .ToLowerInvariant();
    
    public static TBuilder WithKeyDeserializer<TBuilder>(this TBuilder builder, Func<IKafkaConsumerBuilder, object> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new KeyDeserializerMetadata(serializer));
        return builder;
    }

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

    public static TBuilder WithKeyDeserializer<TBuilder, T>(this TBuilder builder, IDeserializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithKeyDeserializer((s) => serializer);
        return builder;
    }

    public static TBuilder WithValueDeserializer<TBuilder>(this TBuilder builder, Func<IKafkaConsumerBuilder, object> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new ValueDeserializerMetadata(serializer));
        return builder;
    }

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

    public static TBuilder WithValueDeserializer<TBuilder, T>(this TBuilder builder, IDeserializer<T> serializer)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithValueDeserializer((s) => serializer);
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

    public static TBuilder WithDefaultTopicOptions<TBuilder>(this TBuilder builder)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new TopicMetadata(
            DefaultNamingConvention,
            null));
        return builder;
    }

    public static TBuilder WithTopicOptions<TBuilder, TMessage>(this TBuilder builder, Func<Type, string>? namingConvention, TimeSpan? retentionPeriod)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new TopicMetadata<TMessage>(
            namingConvention ?? DefaultNamingConvention,
            retentionPeriod));
        return builder;
    }

    public static TBuilder WithTopicOptions<TBuilder, TMessage>(this TBuilder builder, string topicName, TimeSpan? retentionPeriod)
        where TBuilder : IKafkaConventionBuilder
    {
        builder.WithSingle(new TopicMetadata<TMessage>(
            _ => topicName,
            retentionPeriod));
        return builder;
    }
}
