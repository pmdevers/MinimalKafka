using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Producing;

namespace MinimalKafka;

/// <summary>Represents the next delegate in the consumer middleware pipeline.</summary>
/// <param name="context">The current consumer context.</param>
public delegate Task ConsumerDelegate(KafkaContext context);

/// <summary>Represents a Kafka message being consumed.</summary>
/// <param name="consumeResult">The consumed Kafka result.</param>
/// <param name="producer">The producer available for publishing additional messages.</param>
/// <param name="requestServices">The service provider scoped to the handler invocation.</param>
/// <param name="cancellationToken">The token used to cancel processing.</param>
public sealed class KafkaContext(
    ConsumeResult<byte[], byte[]> consumeResult,
    IKafkaProducer producer,
    IServiceProvider requestServices,
    CancellationToken cancellationToken)
{
    /// <summary>The topic the message was consumed from.</summary>
    public string Topic { get; } = consumeResult.Topic;

    /// <summary>The optional message key.</summary>
    public byte[]? Key { get; } = consumeResult.Message.Key;

    /// <summary>The message value.</summary>
    public byte[]? Value { get; set; } = consumeResult.Message.Value;

    private readonly KafkaHeaders _headers = KafkaHeaders.FromConfluent(consumeResult.Message.Headers);

    /// <summary>The Kafka headers for the message.</summary>
    public KafkaHeaders Headers => _headers;

    /// <summary>The source partition for the consumed message.</summary>
    public int Partition { get; } = consumeResult.Partition.Value;

    /// <summary>The source offset for the consumed message.</summary>
    public long Offset { get; } = consumeResult.Offset.Value;

    /// <summary>The CorrelationId header value from the consumed message, when present.</summary>
    public string? CorrelationId => CorrelationHeaders.ReadCorrelationId(Headers);

    /// <summary>The message format configured for this topic, or null to use the default format.</summary>
    public string? Format { get; internal set; }

    /// <summary>The producer available for publishing additional messages.</summary>
    public IKafkaProducer Producer { get; } = producer;

    /// <summary>The service provider scoped to the handler invocation.</summary>
    public IServiceProvider RequestServices { get; } = requestServices;

    /// <summary>The token used to cancel processing.</summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;
}

/// <summary>Configures Kafka consumption and production. Features are added as extension methods on this builder.</summary>
public interface IMinimalKafkaBuilder
{
    /// <summary>The service collection used to register MinimalKafka services.</summary>
    IServiceCollection Services { get; }
}

internal sealed class MinimalKafkaBuilder(IServiceCollection services) : IMinimalKafkaBuilder
{
    public IServiceCollection Services { get; } = services;
}
