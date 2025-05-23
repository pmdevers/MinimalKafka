using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;
using MinimalKafka.Metadata;

namespace MinimalKafka;

public abstract class KafkaConsumer
{
    public abstract ILogger Logger { get; }
    public abstract void Subscribe();
    public abstract KafkaContext Consume(CancellationToken cancellationToken);

    public abstract void Close();

    public static KafkaConsumer Create(KafkaConsumerOptions options)
    {
        var creator = typeof(KafkaConsumer<,>)
            .MakeGenericType(options.KeyType, options.ValueType)
            .GetConstructor([typeof(KafkaConsumerOptions)]);

        return (KafkaConsumer)(creator?.Invoke([options]) ?? new NoConsumer());
    }
}

public class NoConsumer : KafkaConsumer
{
    public override ILogger Logger => throw new NotImplementedException();

    public override void Close()
    {
    }

    public override KafkaContext Consume(CancellationToken cancellationToken)
    {
        return KafkaContext.Empty;
    }

    public override void Subscribe()
    {
    }
}

public class KafkaConsumer<TKey, TValue>(KafkaConsumerOptions options) : KafkaConsumer
{
    private readonly IServiceProvider _serviceProvider = options.ServiceProvider;
    
    private readonly string _topicName = options.Metadata.OfType<ITopicFormatter>()
        .First().Format(options.TopicName);

    private readonly IConsumer<TKey, TValue> _consumer =
        new KafkaConsumerBuilder<TKey, TValue>(options.Metadata, options.ServiceProvider).Build();

    private bool _isClosed = false;
    private long _recordsConsumed;
    private readonly int _consumeReportInterval =
        options.Metadata.OfType<ReportIntervalMetadata>().FirstOrDefault()?.ReportInterval
        ?? 5;

    public override ILogger Logger => options.KafkaLogger;

    public override KafkaContext Consume(CancellationToken cancellationToken)
    {
        try
        {
            var scope = _serviceProvider.CreateScope();
            var result = _consumer.Consume(cancellationToken);

            if (++_recordsConsumed % _consumeReportInterval == 0)
            {
                Logger.RecordsConsumed(options.Metadata.GroupId(), options.Metadata.ClientId(), _recordsConsumed, result.Topic);
            }

            return KafkaContext.Create(result, scope.ServiceProvider, options.Metadata);
        }
        catch (OperationCanceledException ex) 
        when (ex.CancellationToken == cancellationToken)
        {
            Logger.OperatonCanceled(options.Metadata.GroupId(), options.Metadata.ClientId());

            _consumer.Close();
            _consumer.Dispose();
            return KafkaContext.Empty;
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

public static class MetadataHelperExtensions 
{
    public static string ClientId(this IReadOnlyList<object> metadata)
    {
        var meta = metadata.GetMetaData<IClientIdMetadata>()!;
        return meta.ClientId;
    }

    public static string GroupId(this IReadOnlyList<object> metadata)
    {
        var meta = metadata.GetMetaData<IGroupIdMetadata>()!;
        return meta.GroupId;
    }

    private static T? GetMetaData<T>(this IReadOnlyList<object> metaData)
        => metaData.OfType<T>().FirstOrDefault();
}



