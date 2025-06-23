using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Extension;
using MinimalKafka.Helpers;
using MinimalKafka.Metadata;
using MinimalKafka.Metadata.Internals;

namespace MinimalKafka;

/// <summary>
/// Represents an abstract base class for a Kafka consumer, providing methods for subscribing, consuming, and closing the consumer.
/// </summary>
public abstract class KafkaConsumer
{
    /// <summary>
    /// Gets the logger instance used by the consumer.
    /// </summary>
    public abstract ILogger Logger { get; }

    /// <summary>
    /// Subscribes the consumer to the configured topic(s).
    /// </summary>
    public abstract void Subscribe();

    /// <summary>
    /// Consumes a message from the topic and invokes the provided delegate to process the message.
    /// </summary>
    /// <param name="kafkaDelegate">The delegate to invoke for processing the consumed message.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous consume operation.</returns>
    public abstract Task Consume(KafkaDelegate kafkaDelegate, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the consumer and releases any resources.
    /// </summary>
    public abstract void Close();

    /// <summary>
    /// Creates a new <see cref="KafkaConsumer"/> instance for the specified options.
    /// </summary>
    /// <param name="options">The consumer options containing types, topic, metadata, and service provider.</param>
    /// <returns>A concrete <see cref="KafkaConsumer"/> instance.</returns>
    public static KafkaConsumer Create(KafkaConsumerOptions options)
    {
        var creator = typeof(KafkaConsumer<,>)
            .MakeGenericType(options.KeyType, options.ValueType)
            .GetConstructor([typeof(KafkaConsumerOptions)]);

        return (KafkaConsumer)(creator?.Invoke([options]) ?? new NoConsumer());
    }
}

internal sealed class NoConsumer : KafkaConsumer
{
    public override ILogger Logger => throw new NotImplementedException();

    public override void Close()
    {
    }

    public override Task Consume(KafkaDelegate kafkaDelegate, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override void Subscribe()
    {
    }
}

internal sealed class KafkaConsumer<TKey, TValue>(KafkaConsumerOptions options) : KafkaConsumer
{
    private readonly IServiceProvider _serviceProvider = options.ServiceProvider;
    
    private readonly string _topicName = options.Metadata.OfType<ITopicFormatterMetadata>()
        .First().Format(options.TopicName);

    private readonly IConsumer<TKey, TValue> _consumer =
        new KafkaConsumerBuilder<TKey, TValue>(options.Metadata, options.ServiceProvider).Build();

    private bool _isClosed = false;
    private long _recordsConsumed;
    private readonly int _consumeReportInterval =
        options.Metadata.OfType<ReportIntervalMetadata>().FirstOrDefault()?.ReportInterval
        ?? 5;

    public override ILogger Logger => options.KafkaLogger;

    public override async Task Consume(KafkaDelegate kafkaDelegate, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var result = _consumer.Consume(cancellationToken);

            if (++_recordsConsumed % _consumeReportInterval == 0)
            {
                Logger.RecordsConsumed(options.Metadata.GroupId(), options.Metadata.ClientId(), _recordsConsumed, result.Topic);
            }

            var context = KafkaContext.Create(result, scope.ServiceProvider, options.Metadata);

            if (context is EmptyKafkaContext)
            {
                return;
            }

            await kafkaDelegate.Invoke(context);

            if (options.Metadata.IsAutoCommitEnabled())
            {
                return;
            }

            Logger.Committing(options.Metadata.GroupId(), options.Metadata.ClientId());

            _consumer.StoreOffset(result);
            _consumer.Commit();
        }
        catch (OperationCanceledException ex) 
        when (ex.CancellationToken == cancellationToken)
        {
            Logger.OperatonCanceled(options.Metadata.GroupId(), options.Metadata.ClientId());
        }
    }

    public override void Close()
    {
        if(_isClosed)
        {
            Logger.ConsumerAlreadyClosed(options.Metadata.GroupId(), options.Metadata.ClientId());
            return;
        }

        _isClosed = true;
        Logger.ConsumerClosed(options.Metadata.GroupId(), options.Metadata.ClientId());
        _consumer.Close();
        _consumer.Dispose();
    }

    public override void Subscribe()
    {
        Logger.Subscribed(options.Metadata.GroupId(), options.Metadata.ClientId(), _topicName);

        _consumer.Subscribe(_topicName);
    }
}



