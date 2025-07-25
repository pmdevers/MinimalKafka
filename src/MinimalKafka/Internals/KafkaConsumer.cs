﻿using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;

namespace MinimalKafka.Internals;

internal class KafkaConsumerConfig
{
    public required KafkaConsumerKey Key { get; init; }
    public required IReadOnlyList<KafkaDelegate> Delegates { get; init; }
    public required IReadOnlyList<object> Metadata { get; init; }

    internal static KafkaConsumerConfig Create(KafkaConsumerKey key, List<KafkaDelegate> delegates, List<object> metaData)
        => new()
        {
            Key = key,
            Delegates = [.. delegates],
            Metadata = [.. metaData]
        };
}

internal class KafkaConsumer(
    KafkaConsumerConfig config,
    IKafkaProducer producer,
    KafkaTopicFormatter topicFormatter,
    IServiceProvider serviceProvider,
    ILogger<KafkaConsumer> logger) : IKafkaConsumer
{
    private long _recordsConsumed;
    private readonly int _reportInterval = config.Metadata.ReportInterval();
    private readonly bool _autoCommitEnabled = config.Metadata.AutoCommitEnabled();
    private readonly IConsumer<byte[], byte[]> _consumer = CreateConsumer(config.Metadata);

    public void Subscribe()
    {
        var topic = topicFormatter(config.Key.TopicName);
        _consumer.Subscribe(topic);
        logger.Subscribed(config.Key.GroupId, config.Key.ClientId, topic);
    }

    public async Task Consume(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);

                if(result == null) 
                    continue;

                if (++_recordsConsumed % _reportInterval == 0)
                {
                    logger.RecordsConsumed(config.Key.GroupId, config.Key.ClientId, _recordsConsumed, result.Topic);
                }

                var context = KafkaContext.Create(config.Key.TopicName, config.Metadata, result.Message, scope.ServiceProvider);

                var store = context.GetTopicStore();

                await store.AddOrUpdate(context.Key, context.Value);

                foreach (var kafkaDelegate in config.Delegates)
                {
                    await kafkaDelegate.Invoke(context);
                }

                await producer.ProduceAsync(context, cancellationToken);

                Commit(result);
            }
            catch (KafkaException ex) 
            when (ex.Error.Code == ErrorCode.Local_NoOffset)
            {
                logger.NoOffsetStored(config.Key.GroupId, config.Key.ClientId, config.Key.TopicName);
            }
            catch (OperationCanceledException ex)
            when(ex.CancellationToken == cancellationToken)
            {   
                logger.OperatonCanceled(config.Key.GroupId, config.Key.ClientId);
            }
        }
    }

    public void Close()
    {
        if (_isClosed)
        {
            logger.ConsumerAlreadyClosed(config.Key.GroupId, config.Key.ClientId);
            return;
        }

        _isClosed = true;

        _consumer.Close();
        _consumer.Dispose();
        logger.ConsumerClosed(config.Key.GroupId, config.Key.ClientId);
    }

    private bool _isClosed;

    public string TopicName => config.Key.TopicName;

    private void Commit(ConsumeResult<byte[], byte[]> result)
    {
        if (!_autoCommitEnabled)
        {
            logger.Committing(config.Key.GroupId, config.Key.ClientId);

            _consumer.StoreOffset(result);
            _consumer.Commit();
        }
    }

    private static IConsumer<byte[], byte[]> CreateConsumer(IReadOnlyList<object> metadata)
    {
        var config = metadata.ConsumerConfig();
        var handlers = metadata.ConsumerHandlers();

        return new ConsumerBuilder<byte[], byte[]>(config)
            .SetKeyDeserializer(Deserializers.ByteArray)
            .SetValueDeserializer(Deserializers.ByteArray)
            .SetStatisticsHandler(handlers?.StatisticsHandler)
            .SetErrorHandler(handlers?.ErrorHandler)
            .SetLogHandler(handlers?.LogHandler)
            .SetPartitionsAssignedHandler(handlers?.PartitionsAssignedHandler)
            .SetPartitionsLostHandler(handlers?.PartitionsLostHandler)
            .SetPartitionsRevokedHandler(handlers?.PartitionsRevokedHandler)
            .Build();
    }
}

    