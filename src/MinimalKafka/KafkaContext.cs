using Confluent.Kafka;
using MinimalKafka.Internals;

namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
public class KafkaContext
{
    /// <summary>
    /// 
    /// </summary>
    public required IServiceProvider RequestServices { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public required byte[] Key { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public required byte[] Value { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public required IDictionary<string, byte[]> Headers { get; init; }

    internal static KafkaContext Create(Message<byte[], byte[]> message, IServiceProvider serviceProvider)
        => new() { 
            Key = message.Key, 
            Value = message.Value, 
            Headers = message.Headers.ToDictionary(x => x.Key, y => y.GetValueBytes()),
            RequestServices = serviceProvider };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    internal void Produce(KafkaMessage message)
    {
        _messages.Add(message);
    }

    private readonly List<KafkaMessage> _messages = [];
    internal IReadOnlyList<KafkaMessage> Messages => _messages.AsReadOnly();
}

/// <summary>
/// /
/// </summary>
/// <param name="context"></param>
public delegate Task KafkaDelegate(KafkaContext context);
