using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Producing;

namespace MinimalKafka;

public delegate Task ConsumerDelegate(KafkaContext context);

public sealed class KafkaContext(
    ConsumeResult<string, byte[]> consumeResult,
    IMessageProducer producer,
    IServiceProvider requestServices,
    CancellationToken cancellationToken)
{
    public ConsumeResult<string, byte[]> ConsumeResult { get; } = consumeResult;
    public string Topic => ConsumeResult.Topic;
    public string? Key => ConsumeResult.Message.Key;
    public byte[]? Value { get; set; } = consumeResult.Message.Value;
    public Headers Headers => ConsumeResult.Message.Headers;
    /// <summary>The message format configured for this topic, or null to use the default format.</summary>
    public string? Format { get; internal set; }
    public IMessageProducer Producer { get; } = producer;
    public IServiceProvider RequestServices { get; } = requestServices;
    public CancellationToken CancellationToken { get; } = cancellationToken;
}


/// <summary>Configures Kafka consumption and production. Features are added as extension methods on this builder.</summary>
public interface IMinimalKafkaBuilder
{
    IServiceCollection Services { get; }
}

internal sealed class MinimalKafkaBuilder(IServiceCollection services) : IMinimalKafkaBuilder
{
    public IServiceCollection Services { get; } = services;
}