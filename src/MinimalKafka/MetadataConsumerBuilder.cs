using Confluent.Kafka;
using MinimalKafka.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace MinimalKafka;

internal class MetadataConsumerBuilder<TKey, TValue>
{
    private readonly ConsumerBuilder<TKey, TValue> _consumerBuilder;
    private readonly IReadOnlyList<object> _metadata;
    private readonly IServiceProvider _serviceProvider;

    public MetadataConsumerBuilder(IReadOnlyList<object> metadata, IServiceProvider serviceProvider)
    {
        _metadata = metadata;
        _serviceProvider = serviceProvider;

        var config = BuildConfig();
        _consumerBuilder = new ConsumerBuilder<TKey, TValue>(config);
    }

    private ConsumerConfig BuildConfig()
    {
        var config = new ConsumerConfig();
        foreach (var item in _metadata.OfType<IConsumerConfigMetadata>())
        {
            item.Set(config);
        }
        return config;
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

    public IConsumer<TKey, TValue> Build()
    {
        SetDeserializers(_consumerBuilder);
        return _consumerBuilder.Build();
    }

    private void SetDeserializers(ConsumerBuilder<TKey, TValue> builder)
    {
        if (GetMetaData<IKeyDeserializerMetadata>(out var key))
        {
            var keyDeserializer = (IDeserializer<TKey>)key.KeyDeserializer(_serviceProvider, typeof(TKey));
            builder.SetKeyDeserializer(keyDeserializer);
        }

        if (GetMetaData<IValueDeserializerMetadata>(out var value))
        {
            var valueDeserializer = (IDeserializer<TValue>)value.ValueDeserializer(_serviceProvider, typeof(TValue));
            builder.SetValueDeserializer(valueDeserializer);
        }
    }
}
