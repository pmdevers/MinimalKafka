using Confluent.Kafka;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace MinimalKafka;

public interface IKafkaConsumerBuilder
{
    IServiceProvider ServiceProvider { get; }
    Type KeyType { get; }
    Type ValueType { get; }
    IReadOnlyList<object> Metadata { get; }
}

internal class KafkaConsumerBuilder<TKey, TValue> : IKafkaConsumerBuilder
{
    private readonly ConsumerBuilder<TKey, TValue> _consumerBuilder;
    public IServiceProvider ServiceProvider {get; }
    public Type KeyType { get; } = typeof(TKey);

    public Type ValueType { get; } = typeof(TValue);

    public IReadOnlyList<object> Metadata { get; }

    public KafkaConsumerBuilder(IReadOnlyList<object> metadata, IServiceProvider serviceProvider)
    {
        Metadata = metadata;
        ServiceProvider = serviceProvider;

        var config = BuildConfig();
        _consumerBuilder = new ConsumerBuilder<TKey, TValue>(config);
    }

    private ConsumerConfig BuildConfig()
    {
        var c = Metadata.OfType<IConfigurationMetadata>().FirstOrDefault()?.Configuration;

        ConsumerConfig config = c is null ? new() : new(c);

        foreach (var item in Metadata.OfType<IConsumerConfigMetadata>())
        {
            item.Set(config);
        }
        return config;
    }
    private bool GetMetaData<T>([NotNullWhen(true)] out T? metadata)
    {
        metadata = default;
        var m = Metadata.OfType<T>().FirstOrDefault();
        if (m is not null)
        {
            metadata = m;
            return true;
        }
        return false;
    }

    public IConsumer<TKey, TValue> Build()
    {
        SetDeserializers(_consumerBuilder);
        SetHandlers(_consumerBuilder);
        return _consumerBuilder.Build();
    }

    private void SetHandlers(ConsumerBuilder<TKey, TValue> consumerBuilder)
    {
        if(GetMetaData<IConsumerHandlerMetadata>(out var handlers))
        {
            if (handlers.PartitionsAssignedHandler is not null) consumerBuilder.SetPartitionsAssignedHandler(handlers.PartitionsAssignedHandler);
            if (handlers.PartitionsRevokedHandler is not null) consumerBuilder.SetPartitionsRevokedHandler(handlers.PartitionsRevokedHandler);
            if (handlers.PartitionsLostHandler is not null) consumerBuilder.SetPartitionsLostHandler(handlers.PartitionsLostHandler);
            if (handlers.StatisticsHandler is not null) consumerBuilder.SetStatisticsHandler(handlers.StatisticsHandler);
            if (handlers.ErrorHandler is not null) consumerBuilder.SetErrorHandler(handlers.ErrorHandler);
            if (handlers.LogHandler is not null) consumerBuilder.SetLogHandler(handlers.LogHandler);
         }
    }

    private void SetDeserializers(ConsumerBuilder<TKey, TValue> builder)
    {
        if (GetMetaData<KeyDeserializerMetadata>(out var key))
        {
            var keyDeserializer = (IDeserializer<TKey>)key.Deserializer(this);
            builder.SetKeyDeserializer(keyDeserializer);
        }

        if (GetMetaData<ValueDeserializerMetadata>(out var value))
        {
            var valueDeserializer = (IDeserializer<TValue>)value.Deserializer(this);
            builder.SetValueDeserializer(valueDeserializer);
        }
    }
}
