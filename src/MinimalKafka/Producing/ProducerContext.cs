using Confluent.Kafka;

namespace MinimalKafka.Producing;

public delegate Task ProducerDelegate(ProducerContext context);
public sealed class ProducerContext(
    string topic,
    string? key,
    byte[] value,
    Headers headers,
    CancellationToken cancellationToken)
{
    public string Topic { get; set; } = string.IsNullOrWhiteSpace(topic)
            ? throw new ArgumentException("A topic is required.", nameof(topic))
            : topic;
    public string? Key { get; set; } = key;
    public byte[] Value
    {
        get => _value;
        set => _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    private byte[] _value = value ?? throw new ArgumentNullException(nameof(value));
    public Headers Headers { get; } = headers ?? throw new ArgumentNullException(nameof(headers));
    public CancellationToken CancellationToken { get; } = cancellationToken;
    internal DeliveryResult<string, byte[]>? DeliveryResult { get; set; }
}
