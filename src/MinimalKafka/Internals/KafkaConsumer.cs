using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;
using MinimalKafka.Metadata;

namespace MinimalKafka.Internals;

internal class KafkaConsumer(
    KafkaConsumerKey consumerKey,
    bool autoCommitEnabled,
    IConsumer<byte[], byte[]> consumer,
    IKafkaProducer producer,
    KafkaDelegate[] kafkaDelegates,
    IReadOnlyList<object> metadata,
    IServiceProvider serviceProvider,
    ILogger<KafkaConsumer> logger) : IKafkaConsumer
{
    public void Subscribe()
    {
        var topic = consumerKey.TopicName;
        var topicFormatter = metadata.OfType<ITopicFormaterMetadata>().FirstOrDefault();
        if (topicFormatter != null)
        {
            topic = topicFormatter.TopicFormatter.Invoke(topic);
        }

        consumer.Subscribe(topic);
        logger.Subscribed(consumerKey.GroupId, consumerKey.ClientId, consumerKey.TopicName);
    }

    public async Task Consume(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var result = consumer.Consume(cancellationToken);

            var context = KafkaContext.Create(consumerKey, result.Message, scope.ServiceProvider, metadata);

            var store = context.GetTopicStore();

            await store.AddOrUpdate(context.Key, context.Value);

            foreach (var kafkaDelegate in kafkaDelegates)
            {
                await kafkaDelegate.Invoke(context);
            }

            await producer.ProduceAsync(context, cancellationToken);

            Commit(result);
        }
        catch (OperationCanceledException ex)
        when(ex.CancellationToken == cancellationToken)
        {
            
            logger.OperatonCanceled(consumerKey.GroupId, consumerKey.ClientId);
        }
    }

    public void Close()
    {
        if (_isClosed)
        {
            logger.ConsumerAlreadyClosed(consumerKey.GroupId, consumerKey.ClientId);
            return;
        }

        _isClosed = true;

        consumer.Close();
        consumer.Dispose();
        logger.ConsumerClosed(consumerKey.GroupId, consumerKey.ClientId);
    }

    private bool _isClosed;

    private void Commit(ConsumeResult<byte[], byte[]> result)
    {
        if (!autoCommitEnabled)
        {
            logger.Committing(consumerKey.GroupId, consumerKey.ClientId);

            consumer.StoreOffset(result);
            consumer.Commit();
        }
    }
}

    