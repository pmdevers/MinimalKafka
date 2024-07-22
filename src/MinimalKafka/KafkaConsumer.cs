using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pmdevers.MinimalKafka.Metadata;

namespace Pmdevers.MinimalKafka;

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
    private readonly string _topicName = options.TopicName;
    private readonly IConsumer<TKey, TValue> _consumer = CreateConsumer(options);

    private long _recordsConsumed;
    private readonly int _consumeReportInterval =
        options.Metadata.OfType<ReportIntervalMetaData>().FirstOrDefault()?.ReportInterval
        ?? 5;

    public override ILogger Logger => options.KafkaLogger;

    public override KafkaContext Consume(CancellationToken cancellationToken)
    {
        var scope = _serviceProvider.CreateScope();
        var result = _consumer.Consume(cancellationToken);

        if (++_recordsConsumed % _consumeReportInterval == 0)
        {
            Logger.LogInformation("{Records} records consumed so far", _recordsConsumed);
        }

        return KafkaContext.Create(result, scope.ServiceProvider);
    }

    public override void Close()
    {
        Logger.LogInformation("Close() on cunsumer called.");
        _consumer.Close();
        _consumer.Dispose();
    }

    public override void Subscribe()
    {
        _consumer.Subscribe(_topicName);
        Logger.LogInformation("Subscribed to topic: '{Topic}'", _topicName);
    }

    private static IConsumer<TKey, TValue> CreateConsumer(KafkaConsumerOptions options)
    {
        var config = new ConsumerConfig();
        foreach (var item in options.Metadata.OfType<IConsumerConfigMetadata>())
        {
            item.Set(config);
        }

        var builder = new ConsumerBuilder<TKey, TValue>(config);

        builder.MetaData().Set<IKeyDeserializerMetadata>(options, (x, builder) =>
        {
            var deserializer = (IDeserializer<TKey>)x.KeyDeserializer.Invoke(options.ServiceProvider, typeof(TKey));
            builder.SetKeyDeserializer(deserializer);
        });
        builder.MetaData().Set<IValueDeserializerMetadata>(options, (x, builder) =>
        {
            var deserializer = (IDeserializer<TValue>)x.ValueDeserializer.Invoke(options.ServiceProvider, typeof(TValue));
            builder.SetValueDeserializer(deserializer);
        });

        return builder.Build();
    }
}

internal class MetadataConsumerBuilder<TKey, TValue>(ConsumerBuilder<TKey, TValue> builder)
{
    private readonly ConsumerBuilder<TKey, TValue> _builder = builder;

    public MetadataConsumerBuilder<TKey, TValue> Set<TMetadata>(
        KafkaConsumerOptions options,
        Action<TMetadata, ConsumerBuilder<TKey, TValue>> action)
    {
        var metadata = options.Metadata.OfType<TMetadata>().FirstOrDefault();
        if (metadata is not null)
        {
            action.Invoke(metadata, _builder);
        }

        return this;
    }
}

public static class ConsumerBuilderExtensions
{
    internal static MetadataConsumerBuilder<TKey, TValue> MetaData<TKey, TValue>(this ConsumerBuilder<TKey, TValue> builder)
        => new(builder);

}