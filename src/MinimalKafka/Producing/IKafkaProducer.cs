using Confluent.Kafka;

namespace MinimalKafka.Producing;

/// <summary>Produces messages to Kafka topics.</summary>
public interface IKafkaProducer
{
    /// <summary>Serializes and produces a message to the specified Kafka topic.</summary>
    /// <typeparam name="TKey">The .NET type of the message key.</typeparam>
    /// <typeparam name="TValue">The .NET type of the message value.</typeparam>
    /// <param name="topic">The destination topic name.</param>
    /// <param name="key">The optional message key; may be null.</param>
    /// <param name="value">The optional message value; may be null.</param>
    /// <param name="headers">The optional Kafka headers.</param>
    /// <param name="format">The optional message format. When null, the default format is used.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The Kafka delivery result for the produced message.</returns>
    Task<DeliveryResult<byte[], byte[]>> ProduceAsync<TKey, TValue>(
        string topic,
        TKey? key,
        TValue? value,
        Headers? headers = null,
        string? format = null,
        CancellationToken cancellationToken = default);
}

