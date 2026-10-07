using Confluent.Kafka;

namespace MinimalKafka.Serialization;

/// <summary>Converts message values between .NET objects and Kafka payload bytes for one format.</summary>
public interface IMessageSerializer
{
    /// <summary>The format name, see <see cref="MessageFormats"/>.</summary>
    string Format { get; }

    Task<byte[]> SerializeAsync(object value, string topic, Headers headers, CancellationToken cancellationToken);

    Task<object?> DeserializeAsync(byte[] data, Type type, string topic, Headers headers, CancellationToken cancellationToken);
}

public interface IMessageSerializerRegistry
{
    /// <summary>Returns the serializer for <paramref name="format"/>, or the default format when null.</summary>
    IMessageSerializer Get(string? format);
}
