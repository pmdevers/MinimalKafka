using Confluent.Kafka;

namespace MinimalKafka;

public abstract class KafkaContext
{
    public abstract IServiceProvider RequestServices { get; }
    public abstract object? Key { get; }
    public abstract object? Value { get; }
    public abstract Timestamp Timestamp { get; }
    public abstract Headers Headers { get; }

    public static KafkaContext Create(object result, IServiceProvider provider)
    {
        var keyType = result.GetType().GenericTypeArguments[0];
        var valueType = result.GetType().GenericTypeArguments[1];
        var creator = typeof(KafkaContext<,>).MakeGenericType(keyType, valueType)
            .GetConstructor(new[] { result.GetType(), typeof(IServiceProvider) });

        return (KafkaContext)(creator?.Invoke(new[] { result, provider }) ?? new NoKafkaContext());
    }
}

public class NoKafkaContext : KafkaContext
{
    public override IServiceProvider RequestServices => throw new NotImplementedException();

    public override object? Key => throw new NotImplementedException();

    public override object? Value => throw new NotImplementedException();

    public override Timestamp Timestamp => throw new NotImplementedException();

    public override Headers Headers => throw new NotImplementedException();
}

public class KafkaContext<TKey, TValue> : KafkaContext
{
    private readonly ConsumeResult<TKey, TValue> _result;

    public KafkaContext(ConsumeResult<TKey, TValue> result, IServiceProvider serviceProvider)
    {
        _result = result;
        RequestServices = serviceProvider;
    }
    public override IServiceProvider RequestServices { get; }

    public override object? Key => _result.Message.Key;

    public override object? Value => _result.Message.Value;

    public override Timestamp Timestamp => _result.Message.Timestamp;

    public override Headers Headers => _result.Message.Headers;
}
