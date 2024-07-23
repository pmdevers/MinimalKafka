using Confluent.Kafka;

namespace MinimalKafka;
public abstract class KafkaContext
{
    public abstract object? Key { get; }
    public abstract object? Value { get; }
    public abstract Headers Headers { get; }

    public abstract IServiceProvider RequestServices { get; }

    public static KafkaContext Empty { get; } = new EmptyKafkaContext();

    public static KafkaContext Create(object result, IServiceProvider serviceProvider)
    {
        var resultType = result.GetType();
        if (!resultType.IsGenericType)
        {
            return Empty;
        }

        if (resultType.GetGenericTypeDefinition() != typeof(ConsumeResult<,>).GetGenericTypeDefinition())
        {
            return Empty;
        }

        var keyType = resultType.GenericTypeArguments[0];
        var valueType = resultType.GenericTypeArguments[1];


        var creator = typeof(KafkaContext<,>).MakeGenericType(keyType, valueType)
            .GetConstructor([resultType, typeof(IServiceProvider)]);

        return (KafkaContext)(
            creator?.Invoke([result, serviceProvider]) ??
            Empty
        );
    }
}

internal class EmptyKafkaContext : KafkaContext
{
    public override object? Key => null;

    public override object? Value => null;

    public override Headers Headers => [];

    public override IServiceProvider RequestServices => EmptyServiceProvider.Instance;
}

internal class KafkaContext<TKey, TValue>(ConsumeResult<TKey, TValue> result, IServiceProvider serviceProvider) : KafkaContext
{
    public override object? Key => result.Message.Key;

    public override object? Value => result.Message.Value;

    public override Headers Headers => result.Message.Headers;

    public override IServiceProvider RequestServices => serviceProvider;
}