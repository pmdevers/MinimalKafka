using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using MinimalKafka.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace MinimalKafka;

public interface IKafkaProducerBuilder
{
    public IServiceProvider ServiceProvider { get; }
    public Type KeyType { get; }
    public Type ValueType { get; }
}

public class KafkaProducerBuilder<TKey, TValue> : IKafkaProducerBuilder
{
    private readonly ProducerBuilder<TKey, TValue> _producerContext;
    private readonly IReadOnlyList<object> _metadata;
    private readonly IServiceProvider _serviceProvider;

    public IServiceProvider ServiceProvider => _serviceProvider;

    public Type KeyType => typeof(TKey);

    public Type ValueType => typeof(TValue);

    public KafkaProducerBuilder(IReadOnlyList<object> metadata, IServiceProvider serviceProvider)
    {
        _metadata = metadata;
        _serviceProvider = serviceProvider;
        var config = BuildConfig();
        _producerContext = new(config);
    }
    private ProducerConfig BuildConfig()
    {
        var c = _metadata.OfType<IConfigurationMetadata>().FirstOrDefault()?.Configuration;

        ProducerConfig config = c is null ? new() : new(c);

        foreach (var item in _metadata.OfType<IProducerConfigMetadata>())
        {
            item.Set(config);
        }
        return config;
    }
    public KafkaProducerBuilder<TKey, TValue> SetSerializer(Action<ProducerBuilder<TKey, TValue>> callback)
    {
        callback(_producerContext);
        return this;
    }

    public IProducer<TKey, TValue> Build() 
    {
        SetDeserializers(_producerContext);
        return _producerContext.Build();
    }

    private bool GetMetaData<T>([NotNullWhen(true)] out T? metadata)
    {
        metadata = default;
        var m = _metadata.OfType<T>().FirstOrDefault();
        if (m is not null)
        {
            metadata = m;
            return true;
        }
        return false;
    }

    private void SetDeserializers(ProducerBuilder<TKey, TValue> builder)
    {
        if (GetMetaData<KeySerializerMetadata>(out var key))
        {
            var keyDeserializer = (ISerializer<TKey>)key.SerializerType(this);
            builder.SetKeySerializer(keyDeserializer);
        }

        if (GetMetaData<ValueSerializerMetadata>(out var value))
        {
            var valueDeserializer = (ISerializer<TValue>)value.SerializerType(this);
            builder.SetValueSerializer(valueDeserializer);
        }
    }
}