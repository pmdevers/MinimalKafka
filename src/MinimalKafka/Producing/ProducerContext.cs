using Confluent.Kafka;

namespace MinimalKafka.Producing;

/// <summary>Represents the next delegate in the producer middleware pipeline.</summary>
/// <param name="context">The current producer context.</param>
public delegate Task ProducerDelegate(ProducerContext context);

/// <summary>Represents a Kafka message being produced.</summary>
/// <param name="topic">The topic to produce to.</param>
/// <param name="key">The optional message key.</param>
/// <param name="value">The serialized message value.</param>
/// <param name="headers">The Kafka headers for the message.</param>
/// <param name="cancellationToken">The token used to cancel the operation.</param>
public sealed class ProducerContext(
    string topic,
    byte[] key,
    byte[] value,
    Headers headers,
    CancellationToken cancellationToken)
{
    /// <summary>The topic to produce to.</summary>
    public string Topic { get; set; } = string.IsNullOrWhiteSpace(topic)
            ? throw new ArgumentException("A topic is required.", nameof(topic))
            : topic;

    /// <summary>The optional message key.</summary>
    public byte[] Key { get; set; } = key;

    /// <summary>The serialized message value.</summary>
    public byte[] Value
    {
        get => _value;
        set => _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    private byte[] _value = value ?? throw new ArgumentNullException(nameof(value));

    /// <summary>The Kafka headers for the message.</summary>
    public Headers Headers { get; } = headers ?? throw new ArgumentNullException(nameof(headers));

    /// <summary>The token used to cancel the operation.</summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    internal DeliveryResult<byte[], byte[]>? DeliveryResult { get; set; }
}
