using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;
using MinimalKafka.Serializers;
using System.Text;

namespace MinimalKafka;

/// <summary>
/// 
/// </summary>

public class KafkaContext
{
    private readonly Message<byte[], byte[]> _message;

    private KafkaContext(string topic, IReadOnlyList<object> metadata, Message<byte[], byte[]> message, IServiceProvider requestServices)
    {
        TopicName = topic; 
        Metadata = metadata;
        RequestServices = requestServices;
        _message = message;
    }


    /// <summary>
    /// The Unique ConsumerKey
    /// </summary>
    public string TopicName { get; }

    /// <summary>
    /// Thhe service provider.
    /// </summary>
    public IServiceProvider RequestServices { get;}

    /// <summary>
    /// The metadata for this consumer
    /// </summary>
    public IReadOnlyList<object> Metadata { get; }

    /// <summary>
    /// The <see cref="ReadOnlySpan{T}"/> of the message key.
    /// </summary>
    public ReadOnlySpan<byte> Key => _message.Key;
    /// <summary>
    /// The <see cref="ReadOnlySpan{T}"/> of the message value.
    /// </summary>
    public ReadOnlySpan<byte> Value => _message.Value;

    /// <summary>
    /// The kafka message headers.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers => _message.Headers
        .ToDictionary(x => x.Key, y => Encoding.UTF8.GetString(y.GetValueBytes()));

    internal static KafkaContext Create(string topic, IReadOnlyList<object> metadata, Message<byte[], byte[]> message, IServiceProvider serviceProvider)
        => new(topic, metadata, message, serviceProvider);

    internal void Produce(KafkaMessage message)
    {
        _messages.Add(message);
    }

    private readonly List<KafkaMessage> _messages = [];
    internal IReadOnlyList<KafkaMessage> Messages => _messages.AsReadOnly();
    
}

/// <summary>
/// Delegate for handling kafka messages.
/// </summary>
/// <param name="context"></param>
public delegate Task KafkaDelegate(KafkaContext context);


internal sealed class EmptyServiceProvider : IServiceProvider
{
    public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
    public object? GetService(Type serviceType) => null;
}