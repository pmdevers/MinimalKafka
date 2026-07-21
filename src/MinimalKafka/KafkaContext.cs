using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Internals;
using System.Text;

namespace MinimalKafka;

/// <summary>
/// Encapsulates all Information during consume of a message.
/// </summary>
public abstract class KafkaContext(IServiceProvider serviceProvider) : IDisposable
{
    /// <summary>
    /// Creates a new instance of <see cref="KafkaContext"/> with the specified parameters.
    /// </summary>
    /// <returns>A new instance of <see cref="KafkaContext"/>.</returns>
    public static KafkaContext Empty() => new EmptyKafkaContext();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="consumerKey"></param>
    /// <param name="metadata"></param>
    /// <param name="message"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static KafkaContext Create(KafkaConsumerKey consumerKey, IReadOnlyList<object> metadata, Message<byte[], byte[]> message, IServiceProvider serviceProvider)
        => new DefaultKafkaContext(consumerKey, metadata, message, serviceProvider);


    private readonly AsyncServiceScope _serviceScope = serviceProvider.CreateAsyncScope();

    private bool _disposed;

    /// <summary>
    /// The service provider.
    /// </summary>
    public IServiceProvider RequestServices => _serviceScope.ServiceProvider;

    /// <summary>
    /// The name of the topic.
    /// </summary>
    public abstract string TopicName { get; }

    /// <summary>
    /// The client identifier.
    /// </summary>
    public abstract string ClientId { get; }

    /// <summary>
    /// The Consumer group identifier.
    /// </summary>
    public abstract string GroupId { get; }

    /// <summary>
    /// The <see cref="ReadOnlySpan{T}"/> of the message key.
    /// </summary>
    public abstract ReadOnlySpan<byte> Key { get; }

    /// <summary>
    /// The <see cref="ReadOnlySpan{T}"/> of the message value.
    /// </summary>
    public abstract ReadOnlySpan<byte> Value { get; }

    /// <summary>
    /// The kafka message headers.
    /// </summary>
    public abstract IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// The metadata for this consumer.
    /// </summary>
    public abstract IReadOnlyList<object> Metadata { get; }

    internal void Produce(KafkaMessage message)
    {
        _messages.Add(message);
    }

    private readonly List<KafkaMessage> _messages = [];

    internal IReadOnlyList<KafkaMessage> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Releases the resources used by the current instance of the class.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the current instance of the class.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _serviceScope.Dispose();
        }

        _disposed = true;
    }
}

internal sealed class DefaultKafkaContext(KafkaConsumerKey consumerKey, IReadOnlyList<object> metadata, Message<byte[], byte[]> message, IServiceProvider requestServices) : KafkaContext(requestServices)
{
    private readonly Message<byte[], byte[]> _message = message;

    public override string TopicName { get; } = consumerKey.TopicName;

    public override string ClientId { get; } = consumerKey.ClientId;

    public override string GroupId { get; } = consumerKey.GroupId;

    public override IReadOnlyList<object> Metadata { get; } = metadata;

    public override ReadOnlySpan<byte> Key => _message.Key;

    public override ReadOnlySpan<byte> Value => _message.Value;

    public override IReadOnlyDictionary<string, string> Headers => _message.Headers
        .ToDictionary(x => x.Key, y => Encoding.UTF8.GetString(y.GetValueBytes()));
}

internal sealed class EmptyKafkaContext() : KafkaContext(EmptyServiceProvider.Instance)
{
    public override string TopicName => string.Empty;

    public override string ClientId => string.Empty;

    public override string GroupId => string.Empty;

    public override ReadOnlySpan<byte> Key => [];

    public override ReadOnlySpan<byte> Value => [];

    public override IReadOnlyDictionary<string, string> Headers => new Dictionary<string, string>();

    public override IReadOnlyList<object> Metadata => [];
}