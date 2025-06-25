using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;
using MinimalKafka.Metadata.Internals;

namespace MinimalKafka;

internal class KafkaProducerFactory<TKey, TValue> : IProducer<TKey, TValue>
{
    private readonly ITopicFormatterMetadata _topicFormatter;

    public IProducer<TKey, TValue> Producer { get; set; }

    public Handle Handle => Producer.Handle;

    public string Name => Producer.Name;

    public IReadOnlyList<object> Metadata { get; }

    public KafkaProducerFactory(IKafkaBuilder builder)
    {
        Metadata = builder.MetaData;

        var config = BuildConfig();

        _topicFormatter = builder.MetaData.OfType<ITopicFormatterMetadata>().First();

        var keySerializer = builder.MetaData.OfType<KeySerializerMetadata>().First();
        var valueSerializer = builder.MetaData.OfType<ValueSerializerMetadata>().First();

        var serializerKey = ActivatorUtilities.CreateInstance(builder.ServiceProvider, keySerializer.GetSerializerType<TKey>());
        var serializerValue = ActivatorUtilities.CreateInstance(builder.ServiceProvider, valueSerializer.GetSerializerType<TValue>());

        Producer = new ProducerBuilder<TKey, TValue>(config)
            .SetKeySerializer((ISerializer<TKey>)serializerKey)
            .SetValueSerializer((ISerializer<TValue>)serializerValue)
            .Build();
    }

    private ProducerConfig BuildConfig()
    {
        var c = Metadata.OfType<IConfigurationMetadata>().FirstOrDefault()?.Configuration;

        ProducerConfig config = c is null ? new() : new(c);

        foreach (var item in Metadata.OfType<IProducerConfigMetadata>())
        {
            item.Set(config);
        }
        return config;
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        return Producer.ProduceAsync(_topicFormatter.Format(topic), message, cancellationToken);
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(TopicPartition topicPartition, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        return Producer.ProduceAsync(topicPartition, message, cancellationToken);
    }

    public void Produce(string topic, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null)
    {
        Producer.Produce(_topicFormatter.Format(topic), message, deliveryHandler);
    }

    public void Produce(TopicPartition topicPartition, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null)
    {
        Producer.Produce(topicPartition, message, deliveryHandler);
    }

    public int Poll(TimeSpan timeout)
    {
        return Producer.Poll(timeout);
    }

    public int Flush(TimeSpan timeout)
    {
        return Producer.Flush(timeout);
    }

    public void Flush(CancellationToken cancellationToken = default)
    {
        Producer.Flush(cancellationToken);
    }

    public void InitTransactions(TimeSpan timeout)
    {
        Producer.InitTransactions(timeout);
    }

    public void BeginTransaction()
    {
        Producer.BeginTransaction();
    }

    public void CommitTransaction(TimeSpan timeout)
    {
        Producer.CommitTransaction(timeout);
    }

    public void CommitTransaction()
    {
        Producer.CommitTransaction();
    }

    public void AbortTransaction(TimeSpan timeout)
    {
        Producer.AbortTransaction(timeout);
    }

    public void AbortTransaction()
    {
        Producer.AbortTransaction();
    }

    public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout)
    {
        Producer.SendOffsetsToTransaction(offsets, groupMetadata, timeout);
    }

    public int AddBrokers(string brokers)
    {
        return Producer.AddBrokers(brokers);
    }

    public void SetSaslCredentials(string username, string password)
    {
        Producer.SetSaslCredentials(username, password);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Producer?.Dispose();
    }

    ~KafkaProducerFactory()
    {
        Dispose(false);
    }
}
