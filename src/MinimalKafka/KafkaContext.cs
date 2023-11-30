using Confluent.Kafka;

namespace MinimalKafka;

public abstract class KafkaContext
{
    public abstract IServiceProvider RequestServices { get; }
    public abstract object? Key { get; }
    public abstract object? Value { get; }
    public abstract Timestamp Timestamp { get; }
    public abstract Headers Headers { get; }
}

public class KafkaContext<TKey, TValue> : KafkaContext
{
    private readonly Message<TKey, TValue> _message;

    public KafkaContext(Message<TKey, TValue> message, IServiceProvider serviceProvider)
    {
        _message = message;
        RequestServices = serviceProvider;
    }
    public override IServiceProvider RequestServices { get; }

    public override object? Key => _message.Key;

    public override object? Value => _message.Value;

    public override Timestamp Timestamp => _message.Timestamp;

    public override Headers Headers => _message.Headers;
}
