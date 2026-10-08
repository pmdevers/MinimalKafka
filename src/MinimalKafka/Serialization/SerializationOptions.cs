using System.Text.Json;

namespace MinimalKafka.Serialization;

/// <summary>Configures message serialization behavior.</summary>
public sealed class SerializationOptions
{
    /// <summary>The format used when a topic or a produce call does not specify one.</summary>
    public string DefaultFormat { get; set; } = MessageFormats.Json;

    /// <summary>The JSON serializer options used for the JSON message format.</summary>
    public JsonSerializerOptions Json { get; set; } = new();
}
