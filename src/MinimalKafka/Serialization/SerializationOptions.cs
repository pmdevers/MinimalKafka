using System.Text.Json;

namespace MinimalKafka.Serialization;

public sealed class SerializationOptions
{
    /// <summary>The format used when a topic or a produce call does not specify one.</summary>
    public string DefaultFormat { get; set; } = MessageFormats.Json;

    public JsonSerializerOptions Json { get; set; } = new();
}
