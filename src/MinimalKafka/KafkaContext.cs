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

    private KafkaContext(KafkaConsumerConfig config, Message<byte[], byte[]> message, IServiceProvider requestServices)
    {
        ConsumerKey = config.Key; 
        Metadata = config.Metadata;
        RequestServices = requestServices;
        _message = message;
    }


    /// <summary>
    /// 
    /// </summary>
    public KafkaConsumerKey ConsumerKey { get; }

    /// <summary>
    /// 
    /// </summary>
    public IServiceProvider RequestServices { get;}

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<object> Metadata { get; }

    /// <summary>
    /// 
    /// </summary>
    public ReadOnlySpan<byte> Key => _message.Key;
    /// <summary>
    /// 
    /// </summary>
    public ReadOnlySpan<byte> Value => _message.Value;

    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers => _message.Headers
        .ToDictionary(x => x.Key, y => Encoding.UTF8.GetString(y.GetValueBytes()));

    internal static KafkaContext Create(KafkaConsumerConfig config, Message<byte[], byte[]> message, IServiceProvider serviceProvider)
        => new(config, message, serviceProvider);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetKey<T>()
    {
        var serializer = RequestServices.GetRequiredService<IKafkaSerializer<T>>();
        return serializer.Deserialize(Key);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetValue<T>()
    {
        var serializer = RequestServices.GetRequiredService<IKafkaSerializer<T>>();
        return serializer.Deserialize(Value);
    }

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


internal sealed class EmptyServiceProvider : IServiceProvider
{
    public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
    public object? GetService(Type serviceType) => null;
}