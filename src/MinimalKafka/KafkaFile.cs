using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalKafka;


/// <summary>
/// Describes a File to be transported via kafka.
/// </summary>
/// <param name="Id">The a unique identitifie of the filer</param>
/// <param name="Filename">The name of the file</param>
/// <param name="ContentType">The content type of the file</param>
/// <param name="Data">The Byte Array of the file.</param>
[JsonConverter(typeof(KafkaFileConverter))]
public record KafkaFile(Guid Id, string Filename, string ContentType, ReadOnlyMemory<byte> Data)
{
    /// <summary>
    /// Unique Identifier
    /// </summary>
    public Guid Id { get; set; } = Id;

    /// <summary>
    /// Byte Array of the file
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; set; } = Data;

    /// <summary>
    /// The Name of the file.
    /// </summary>
    public string Filename { get; set; } = Filename;

    /// <summary>
    /// The content type.
    /// </summary>
    public string ContentType { get; set; } = ContentType;


    /// <summary>
    /// Creates a new instance of a KafkaFile
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="contentType"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static KafkaFile Create(string filename, string contentType, ReadOnlyMemory<byte>? data = null)
        => new(Guid.NewGuid(), filename, contentType, data ?? ReadOnlyMemory<byte>.Empty);

    /// <summary>
    /// Represents a Empty instance
    /// </summary>
    public static KafkaFile Empty { get; } = default!;


    internal class KafkaFileConverter : JsonConverter<KafkaFile>
    {
        public override KafkaFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            return new KafkaFile(
                root.GetProperty("id").GetGuid(),
                root.GetProperty("filename").GetString() ?? string.Empty,
                root.GetProperty("contentType").GetString() ?? string.Empty,
                 ReadOnlyMemory<byte>.Empty);
        }

        public override void Write(Utf8JsonWriter writer, KafkaFile value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("id", value.Id);
            writer.WriteString("filename", value.Filename);
            writer.WriteString("contentType", value.ContentType);
            writer.WriteNumber("length", value.Data.Length);
            writer.WriteEndObject();
        }
    }
}

