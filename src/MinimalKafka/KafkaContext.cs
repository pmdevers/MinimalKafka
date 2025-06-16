using Confluent.Kafka;

namespace MinimalKafka;

/// <summary>
/// Represents the context for a Kafka message, providing access to the message key, value, headers, metadata, and request services.
/// </summary>
public abstract class KafkaContext
{
    /// <summary>
    /// Gets the key of the Kafka message.
    /// </summary>
    public abstract object? Key { get; }

    /// <summary>
    /// Gets the value of the Kafka message.
    /// </summary>
    public abstract object? Value { get; }

    /// <summary>
    /// Gets the headers of the Kafka message.
    /// </summary>
    public abstract Headers Headers { get; }

    /// <summary>
    /// Gets the metadata associated with the Kafka message.
    /// </summary>
    public abstract IReadOnlyList<object> MetaData { get; }

    /// <summary>
    /// Gets the service provider for resolving dependencies within the context of the request.
    /// </summary>
    public abstract IServiceProvider RequestServices { get; }

    /// <summary>
    /// Gets the timestamp of the Kafka message.
    /// </summary>
    public abstract DateTime Timestamp { get; }

    /// <summary>
    /// Gets an empty Kafka context instance.
    /// </summary>
    public static KafkaContext Empty { get; } = new EmptyKafkaContext();

    /// <summary>
    /// Creates a new <see cref="KafkaContext"/> instance for the specified result, service provider, and metadata.
    /// </summary>
    /// <param name="result">The consumed Kafka result object.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <param name="metadata">The metadata associated with the message.</param>
    /// <returns>A concrete <see cref="KafkaContext"/> instance or <see cref="Empty"/> if the result is not valid.</returns>
    public static KafkaContext Create(object result, IServiceProvider serviceProvider, IReadOnlyList<object> metadata)
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
            .GetConstructor([resultType, typeof(IServiceProvider), typeof(IReadOnlyList<object>)]);

        return (KafkaContext)(
            creator?.Invoke([result, serviceProvider, metadata]) ??
            Empty
        );
    }
}

internal sealed class EmptyKafkaContext : KafkaContext
{
    public override object? Key => null;

    public override object? Value => null;

    public override Headers Headers => [];

    public override IServiceProvider RequestServices => EmptyServiceProvider.Instance;

    public override IReadOnlyList<object> MetaData => [];

    public override DateTime Timestamp => TimeProvider.System.GetUtcNow().DateTime;
}

internal sealed class KafkaContext<TKey, TValue>(
    ConsumeResult<TKey, TValue> result, 
    IServiceProvider serviceProvider, 
    IReadOnlyList<object> metadata) : KafkaContext
{
    public override object? Key => result.Message.Key;

    public override object? Value => result.Message.Value;

    public override Headers Headers => result.Message.Headers;

    public override IServiceProvider RequestServices => serviceProvider;

    public override IReadOnlyList<object> MetaData => metadata;

    public override DateTime Timestamp => result.Message.Timestamp.UtcDateTime;
}
