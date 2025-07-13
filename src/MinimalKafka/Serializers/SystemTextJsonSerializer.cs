using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MinimalKafka.Serializers;

internal class SystemTextJsonSerializer<T>(JsonSerializerOptions? options = null) : IKafkaSerializer<T>
{
    private readonly JsonSerializerOptions _jsonOptions = options ??
        new(JsonSerializerDefaults.Web);

    public T Deserialize(ReadOnlySpan<byte> value)
    {
        if (value.Length >= 3 && value[..3].SequenceEqual(Utf8Constants.BOM))
            value = value[3..];

        if(value.Length == 0)
        {
            return default!;
        }

        var result = JsonSerializer.Deserialize<T>(value, _jsonOptions);

        return (result ?? default)!;
    }

    public byte[] Serialize(T value)
    {
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, value, typeof(T));
        return stream.ToArray();
    }
}

/// <summary>
/// 
/// </summary>
public static class AddKafkaBuilderExtensions
{
    /// <summary>
    /// Configures the specified <see cref="IKafkaConfigBuilder"/> to use JSON-based serializers and deserializers for
    /// Kafka message keys and values.
    /// </summary>
    /// <remarks>This method registers JSON serializers and deserializers for both keys and values in Kafka
    /// messages. The serializers use the <see cref="JsonSerializerOptions"/> provided via the <paramref
    /// name="options"/> parameter, or default to <see cref="JsonSerializerDefaults.Web"/> if no options are
    /// specified.</remarks>
    /// <param name="builder">The <see cref="IKafkaConfigBuilder"/> to configure.</param>
    /// <param name="options">An optional action to configure the <see cref="JsonSerializerOptions"/> used by the JSON serializers. If not
    /// provided, the default options for <see cref="JsonSerializerDefaults.Web"/> will be used.</param>
    /// <returns>The configured <see cref="IKafkaConfigBuilder"/> instance, allowing for further chaining of configuration methods.</returns>
    public static IKafkaConfigBuilder WithJsonSerializers(this IKafkaConfigBuilder builder, Action<JsonSerializerOptions>? options = null)
    {
        var defaults = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options?.Invoke(defaults);

        builder.Services.AddSingleton<ISerializerFactory>(new SystemTextJsonSerializerFactory(defaults));

        return builder;
    }
}
