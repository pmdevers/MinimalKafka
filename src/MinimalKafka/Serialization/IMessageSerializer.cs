using Confluent.Kafka;

namespace MinimalKafka.Serialization;

/// <summary>Converts message values between .NET objects and Kafka payload bytes for one format.</summary>
public interface IMessageSerializer
{
    /// <summary>The format name, see <see cref="MessageFormats"/>.</summary>
    string Format { get; }

    /// <summary>Serializes a message value for Kafka transport.</summary>
    /// <param name="value">The .NET value to serialize.</param>
    /// <param name="topic">The destination topic name.</param>
    /// <param name="headers">The Kafka headers for the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The serialized payload bytes.</returns>
    Task<byte[]> SerializeAsync(object value, string topic, Headers headers, CancellationToken cancellationToken);

    /// <summary>Deserializes Kafka payload bytes into a .NET value.</summary>
    /// <param name="data">The Kafka payload bytes.</param>
    /// <param name="type">The target .NET type.</param>
    /// <param name="topic">The source topic name.</param>
    /// <param name="headers">The Kafka headers for the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The deserialized value.</returns>
    Task<object?> DeserializeAsync(byte[] data, Type type, string topic, Headers headers, CancellationToken cancellationToken);
}

/// <summary>Resolves message serializers by format name.</summary>
public interface IMessageSerializerRegistry
{
    /// <summary>Returns the serializer for <paramref name="format"/>, or the default format when null.</summary>
    IMessageSerializer Get(string? format);
}
