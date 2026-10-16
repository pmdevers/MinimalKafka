using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MinimalKafka.Serialization;

internal sealed class JsonMessageSerializer(IOptions<SerializationOptions> options) : IKafkaSerializer
{
    public string Format => MessageFormats.Json;

    public Task<byte[]> SerializeAsync(object value, string topic, Headers headers, CancellationToken cancellationToken) =>
        Task.FromResult(JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), options.Value.Json));

    public Task<object?> DeserializeAsync(byte[] data, Type type, string topic, Headers headers, CancellationToken cancellationToken) =>
        Task.FromResult(JsonSerializer.Deserialize(data, type, options.Value.Json));
}
