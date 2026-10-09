using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serialization;
using System.Text;

namespace MinimalKafka.Stream;

internal static class StreamSerialization
{
    public static async Task<T?> DeserializeAsync<T>(
        IServiceProvider services,
        byte[]? data,
        string topic,
        Headers headers,
        CancellationToken cancellationToken,
        string? format)
    {
        if (data is null)
        {
            return default;
        }

        var value = await DeserializeAsync(services, data, typeof(T), topic, headers, cancellationToken, format)
            .ConfigureAwait(false);
        return value is null ? default : (T?)value;
    }

    public static Task<object?> DeserializeAsync(
        IServiceProvider services,
        byte[] data,
        Type type,
        string topic,
        Headers headers,
        CancellationToken cancellationToken,
        string? format)
    {
        if (type == typeof(byte[]))
        {
            return Task.FromResult<object?>(data);
        }

        if (type == typeof(string))
        {
            return Task.FromResult<object?>(Encoding.UTF8.GetString(data));
        }

        var serializer = services.GetRequiredService<IKafkaSerializerRegistry>().Get(format);
        return serializer.DeserializeAsync(data, type, topic, headers, cancellationToken);
    }

    public static Task<byte[]> SerializeAsync<T>(
        IServiceProvider services,
        string topic,
        T value,
        Headers headers,
        CancellationToken cancellationToken,
        string? format)
    {
        return value switch
        {
            byte[] bytes => Task.FromResult(bytes),
            string text => Task.FromResult(Encoding.UTF8.GetBytes(text)),
            _ => services.GetRequiredService<IKafkaSerializerRegistry>()
                .Get(format)
                .SerializeAsync(value!, topic, headers, cancellationToken)
        };
    }
}
