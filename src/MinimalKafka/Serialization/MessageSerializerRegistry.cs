using Microsoft.Extensions.Options;

namespace MinimalKafka.Serialization;

internal sealed class MessageSerializerRegistry : IMessageSerializerRegistry
{
    private readonly Dictionary<string, IMessageSerializer> _serializers;
    private readonly string _defaultFormat;

    public MessageSerializerRegistry(IEnumerable<IMessageSerializer> serializers, IOptions<SerializationOptions> options)
    {
        _serializers = new Dictionary<string, IMessageSerializer>(StringComparer.OrdinalIgnoreCase);
        // Later registrations replace earlier ones, so a custom serializer can override a built-in format.
        foreach (var serializer in serializers)
        {
            _serializers[serializer.Format] = serializer;
        }
        _defaultFormat = options.Value.DefaultFormat;
    }

    public IMessageSerializer Get(string? format)
    {
        var name = string.IsNullOrWhiteSpace(format) ? _defaultFormat : format;
        return _serializers.TryGetValue(name, out var serializer)
            ? serializer
            : throw new InvalidOperationException(
                $"No serializer is registered for format '{name}'. Registered formats: {string.Join(", ", _serializers.Keys)}.");
    }
}
