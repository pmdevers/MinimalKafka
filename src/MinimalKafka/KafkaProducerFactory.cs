using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Metadata;
using System.Globalization;
using TopicMetadata = MinimalKafka.Metadata.TopicMetadata;

namespace MinimalKafka;

public class KafkaProducerFactory<TKey, TValue> : IProducer<TKey, TValue>, IMinimalProducer<TKey, TValue>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    
    private readonly IAdminClient _adminClient;
    
    private readonly ITopicMetadata _topicMetadata;

    private bool _initialized;

    public IProducer<TKey, TValue> Producer { get; set; }

    public Handle Handle => Producer.Handle;

    public string Name => Producer.Name;

    public KafkaProducerFactory(IKafkaBuilder builder, IAdminClient adminClient)
    {
        _adminClient = adminClient;
        var config = builder.MetaData.OfType<ConfigurationMetadata>().First();
        var keySerializer = builder.MetaData.OfType<KeySerializerMetadata>().First(); 
        var valueSerializer = builder.MetaData.OfType<ValueSerializerMetadata>().First();
        var producerConfig = new ProducerConfig(config.Configuration);

        var serializerKey = ActivatorUtilities.CreateInstance(builder.ServiceProvider, keySerializer.GetSerializerType<TKey>());
        var serializerValue = ActivatorUtilities.CreateInstance(builder.ServiceProvider, valueSerializer.GetSerializerType<TValue>());
        
        _topicMetadata = builder.MetaData.OfType<TopicMetadata<TValue>>().FirstOrDefault()
                ?? builder.MetaData.OfType<TopicMetadata>().First();
        
        Producer = new ProducerBuilder<TKey, TValue>(producerConfig)
            .SetKeySerializer((ISerializer<TKey>)serializerKey)
            .SetValueSerializer((ISerializer<TValue>)serializerValue)
            .Build();
    }

    private async Task Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var topicName = _topicMetadata.NamingConvention.Invoke(typeof(TValue));
        
        var metadata = _adminClient.GetMetadata(topicName, Timeout);
        if (metadata.Topics.Exists(topic => topic.Topic == topicName))
        {
            return;
        }

        var config = new Dictionary<string, string>();

        if (_topicMetadata.RetentionPeriod.HasValue)
        {
            config["retention.ms"] = _topicMetadata.RetentionPeriod.Value.TotalMilliseconds
                .ToString(CultureInfo.InvariantCulture);
        }

        await _adminClient.CreateTopicsAsync([new TopicSpecification
        {
            Name = topicName,
            Configs = config
        }]);
    }

    public async Task<DeliveryResult<TKey, TValue>> ProduceAsync(Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        await Initialize();
        return await Producer.ProduceAsync(_topicMetadata.NamingConvention.Invoke(typeof(TValue)), message, cancellationToken);
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        return Producer.ProduceAsync(topic, message, cancellationToken);
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(TopicPartition topicPartition, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        return Producer.ProduceAsync(topicPartition, message, cancellationToken);
    }

    public void Produce(string topic, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null)
    {
        Producer.Produce(topic, message, deliveryHandler);
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
