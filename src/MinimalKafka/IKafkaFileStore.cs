using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalKafka;

internal class NoKafkaFileStore : IKafkaFileStore
{
    public Task<KafkaFile> LoadData(KafkaFile kafkaFile)
        => Task.FromResult(kafkaFile);

    public Task StoreAsync(KafkaFile kafkaFile)
        => Task.CompletedTask;
}


/// <summary>
/// Hydration and De-Hydration logic for kafka files
/// </summary>
public static class KafkaStoreExtensions
{
    extension(IKafkaFileStore store)
    {
        /// <summary>
        /// Calls StoreAsync for all Properties of type KafkaFile.
        /// </summary>
        /// <param name="obj">The Model To Produce</param>
        public async Task DeHydrate(object? obj)
        {
            if (obj is null)
                return;

            var props = obj.GetType().GetProperties()
            .Where(x => x.PropertyType == typeof(KafkaFile));

            foreach (var prop in props)
            {
                if (prop.GetValue(obj, null) is not KafkaFile file)
                {
                    continue;
                }
                await store.StoreAsync(file);
            }
        }

        /// <summary>
        /// This will reload the all Data Fields for Properties of type KafkaFile.
        /// </summary>
        /// <param name="obj">The Model to Hydrate.</param>
        public async Task ReHydrate(object? obj)
        {
            if (obj is null)
                return;

            var props = obj.GetType().GetProperties()
            .Where(x => x.PropertyType == typeof(KafkaFile));

            foreach (var prop in props)
            {
                if (prop.GetValue(obj, null) is not KafkaFile file)
                {
                    continue;
                }

                file = await store.LoadData(file);
                prop.SetValue(obj, file);
            }
        }
    }
}

/// <summary>
/// Describes a class that can store kafka files.
/// </summary>
public interface IKafkaFileStore
{
    /// <summary>
    /// Stores a KafkaFile in the Store.
    /// </summary>
    /// <param name="kafkaFile">Object describing a Binnary File.</param>
    /// <returns></returns>
    Task StoreAsync(KafkaFile kafkaFile);

    /// <summary>
    /// Loads the Data of the KafkaFile from the store.
    /// </summary>
    /// <param name="kafkaFile"></param>
    /// <returns></returns>
    Task<KafkaFile> LoadData(KafkaFile kafkaFile);
}


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
}

internal class KafkaFileConverter : JsonConverter<KafkaFile>
{
    public override KafkaFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
            return null;

        var value = reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            _ => throw new NotSupportedException($"TokenType: '{reader.TokenType}' not supported for '{typeToConvert}'")
        };

        if (string.IsNullOrWhiteSpace(value))
            return KafkaFile.Empty;

        var parts = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return KafkaFile.Empty;

        var identifier = ReadPart(parts[0], nameof(KafkaFile.Id));
        var filename = ReadPart(parts[1], nameof(KafkaFile.Filename));
        var contentType = ReadPart(parts[2], nameof(KafkaFile.ContentType));
        var lengthValue = ReadPart(parts[3], nameof(KafkaFile.Data.Length));

        if (!Guid.TryParse(identifier, out var id) || filename is null || contentType is null || !int.TryParse(lengthValue, out var length) || length < 0)
            return KafkaFile.Empty;

        return new KafkaFile(id, filename, contentType, ReadOnlyMemory<byte>.Empty);
    }

    public override void Write(Utf8JsonWriter writer, KafkaFile value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{nameof(KafkaFile.Id)}={value.Id}; {nameof(KafkaFile.Filename)}={value.Filename}; {nameof(KafkaFile.ContentType)}={value.ContentType}; {nameof(KafkaFile.Data.Length)}={value.Data.Length}");
    }

    private static string? ReadPart(string part, string name)
    {
        var prefix = $"{name}=";

        if (!part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return part[prefix.Length..].Trim();
    }
}