using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;
using System.Diagnostics.Contracts;
using System.Text;

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

internal sealed class KafkaConsumer(
    KafkaConsumerKey key,
    IReadOnlyList<object> metadata,
    IServiceProvider serviceProvider,
    ILogger<KafkaConsumer> logger) : IKafkaConsumer, IDisposable
{
    private readonly IConsumer<byte[], byte[]> _consumer = CreateConsumer(metadata);
    private bool _disposed;

    public void Subscribe()
    {
        _consumer.Subscribe(key.TopicName);
        logger.Subscribed(key.GroupId, key.ClientId, key.TopicName);
    }

    public Task<KafkaContext> Consume(CancellationToken cancellationToken)
    {
        try
        {
            var result = _consumer.Consume(cancellationToken);

            var kafkaMessage = new KafkaMessage(key.TopicName, result.Message.Key, result.Message.Value,
                result.Message.Headers.ToDictionary(h => h.Key, h => Encoding.UTF8.GetString(h.GetValueBytes())))
            {
                Partition = result.Partition.Value,
                Offset = result.Offset.Value
            };

            var context = KafkaContext.Create(key, metadata, kafkaMessage, serviceProvider);
            return Task.FromResult(context);
        }
        catch (KafkaException ex)
        when (ex.Error.Code == ErrorCode.Local_NoOffset)
        {
            logger.NoOffsetStored(key.GroupId, key.ClientId, key.TopicName);
            return Task.FromResult(KafkaContext.Empty());
        }
        catch (OperationCanceledException ex)
        when (ex.CancellationToken == cancellationToken)
        {
            logger.OperatonCanceled(key.GroupId, key.ClientId);
            return Task.FromResult(KafkaContext.Empty());
        }
    }

    public string TopicName => key.TopicName;

    public void Commit(KafkaContext context)
    {
        if (context is EmptyKafkaContext)
        {
            return;
        }

        logger.Committing(key.GroupId, key.ClientId, context.TopicName, context.Partition, context.Offset + 1);

        _consumer.Commit([
            new TopicPartitionOffset(
                context.TopicName,
                new Partition(context.Partition),
                new Offset(context.Offset + 1))
        ]);
    }

    public void Close()
    {
        if (_disposed)
        {
            return;
        }
        _consumer.Close();
        _consumer.Dispose();
        logger.ConsumerClosed(key.GroupId, key.ClientId);

        _disposed = true;
    }

    public void Dispose()
    {
        Close();
    }

    [Pure]
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
            .SetOAuthBearerTokenRefreshHandler(handlers?.OAuthBearerTokenRefreshHandler)
            .Build();
    }
}

