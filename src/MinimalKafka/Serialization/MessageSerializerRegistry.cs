using Microsoft.Extensions.Options;

namespace MinimalKafka.Serialization;

internal sealed class MessageSerializerRegistry : IKafkaSerializerRegistry
{
    private readonly Dictionary<string, IKafkaSerializer> _serializers;
    private readonly string _defaultFormat;

    public MessageSerializerRegistry(IEnumerable<IKafkaSerializer> serializers, IOptions<SerializationOptions> options)
    {
        _serializers = new Dictionary<string, IKafkaSerializer>(StringComparer.OrdinalIgnoreCase);
        // Later registrations replace earlier ones, so a custom serializer can override a built-in format.
        foreach (var serializer in serializers)
        {
            _serializers[serializer.Format] = serializer;
        }
        _defaultFormat = options.Value.DefaultFormat;
    }

    public IKafkaSerializer Get(string? format)
    {
        var name = string.IsNullOrWhiteSpace(format) ? _defaultFormat : format;
        return _serializers.TryGetValue(name, out var serializer)
            ? serializer
            : throw new InvalidOperationException(
                $"No serializer is registered for format '{name}'. Registered formats: {string.Join(", ", _serializers.Keys)}.");
    }
}
