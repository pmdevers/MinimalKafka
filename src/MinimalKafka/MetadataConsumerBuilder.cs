using Confluent.Kafka;
using MinimalKafka.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace MinimalKafka;

internal class MetadataConsumerBuilder<TKey, TValue>
{
    private readonly KafkaConsumerOptions _options;
    private readonly ConsumerBuilder<TKey, TValue> _consumerBuilder;

    public MetadataConsumerBuilder(KafkaConsumerOptions options)
    {
        _options = options;
        var config = BuildConfig();
        _consumerBuilder = new ConsumerBuilder<TKey, TValue>(config);
    }

    private ConsumerConfig BuildConfig()
    {
        var config = new ConsumerConfig();
        foreach (var item in _options.Metadata.OfType<IConsumerConfigMetadata>())
        {
            item.Set(config);
        }
        return config;
    }
    private bool GetMetaData<T>([NotNullWhen(true)] out T? metadata)
    {
        metadata = default;
        var m = _options.Metadata.OfType<T>().FirstOrDefault();
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
        return _consumerBuilder.Build();
    }

    private void SetDeserializers(ConsumerBuilder<TKey, TValue> builder)
    {
        if (GetMetaData<IKeyDeserializerMetadata>(out var key))
        {
            var keyDeserializer = (IDeserializer<TKey>)key.KeyDeserializer(_options.ServiceProvider, typeof(TKey));
            builder.SetKeyDeserializer(keyDeserializer);
        }

        if (GetMetaData<IValueDeserializerMetadata>(out var value))
        {
            var valueDeserializer = (IDeserializer<TValue>)value.ValueDeserializer(_options.ServiceProvider, typeof(TKey));
            builder.SetValueDeserializer(valueDeserializer);
        }
    }
}
