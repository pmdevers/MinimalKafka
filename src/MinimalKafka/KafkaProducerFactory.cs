using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Metadata;
using System.Collections.Concurrent;

namespace MinimalKafka;

public class KafkaProducerFactory(IServiceProvider serviceProvider, IReadOnlyList<object> metadata)
{
    private static ConcurrentDictionary<int, object> _producers { get; } = [];

    public IReadOnlyList<object> MetaData => metadata;
    public IServiceProvider RequestServices => serviceProvider;
    
    public IProducer<TKey, TValue> Create<TKey, TValue>()
    {
        var config = BuildConfig();

        var keySerializer = MetaData.OfType<KeySerializerMetadata>().First();
        var valueSerializer = MetaData.OfType<ValueSerializerMetadata>().First();

        var serializerKey = ActivatorUtilities.CreateInstance(RequestServices, keySerializer.GetSerializerType<TKey>());
        var serializerValue = ActivatorUtilities.CreateInstance(RequestServices, valueSerializer.GetSerializerType<TValue>());

        var producerKey = typeof(TKey).GetHashCode() + typeof(TValue).GetHashCode();

        if(_producers.TryGetValue(producerKey, out var producer))
        {
            return (IProducer<TKey, TValue>)producer;
        }
                
        var kp = new ProducerBuilder<TKey, TValue>(config)
            .SetKeySerializer((ISerializer<TKey>)serializerKey)
            .SetValueSerializer((ISerializer<TValue>)serializerValue)
            .Build();

        _producers.TryAdd(producerKey, kp);
        
        return kp;
    }

    private ProducerConfig BuildConfig()
    {
        var c = MetaData.OfType<IConfigurationMetadata>().FirstOrDefault()?.Configuration;

        ProducerConfig config = c is null ? new() : new(c);

        foreach (var item in MetaData.OfType<IProducerConfigMetadata>())
        {
            item.Set(config);
        }
        return config;
    }
}

public class KafkaProducer<TKey, TValue> : IProducer<TKey, TValue>
{
    private readonly IProducer<TKey, TValue> _producer;

    public KafkaProducer(KafkaProducerFactory factory) : this(factory.Create<TKey, TValue>())
    {
    }

    private KafkaProducer(IProducer<TKey, TValue> producer)
    {
        _producer = producer;
    }

    public Handle Handle => _producer.Handle;

    public string Name => _producer.Name;

    public void AbortTransaction(TimeSpan timeout)
    {
        _producer.AbortTransaction(timeout);
    }

    public void AbortTransaction()
    {
        _producer?.AbortTransaction();
    }

    public int AddBrokers(string brokers)
    {
        return _producer.AddBrokers(brokers);
    }

    public void BeginTransaction()
    {
        _producer.BeginTransaction();
    }

    public void CommitTransaction(TimeSpan timeout)
    {
        _producer.CommitTransaction(timeout);
    }

    public void CommitTransaction()
    {
        _producer.CommitTransaction();
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if(!_disposed && disposing)
        {
            _producer.Dispose();
            _disposed = true;
        }
    }

    ~KafkaProducer()
    {
        Dispose(false);
    }

    public int Flush(TimeSpan timeout)
    {
        return _producer.Flush(timeout);   
    }

    public void Flush(CancellationToken cancellationToken = default)
    {
        _producer.Flush(cancellationToken);
    }

    public void InitTransactions(TimeSpan timeout)
    {
        _producer.InitTransactions(timeout);
    }

    public int Poll(TimeSpan timeout)
    {
        return _producer.Poll(timeout);
    }

    public void Produce(string topic, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null)
    {
        _producer.Produce(topic, message, deliveryHandler);
    }

    public void Produce(TopicPartition topicPartition, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null)
    {
        _producer.Produce(topicPartition, message, deliveryHandler);
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        return _producer.ProduceAsync(topic, message, cancellationToken);  
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(TopicPartition topicPartition, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        return _producer.ProduceAsync(topicPartition, message, cancellationToken);
    }

    public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout)
    {
        _producer.SendOffsetsToTransaction(offsets, groupMetadata, timeout);
    }

    public void SetSaslCredentials(string username, string password)
    {
        _producer.SetSaslCredentials(username, password);
    }
}
