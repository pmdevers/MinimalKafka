namespace MinimalKafka.Serialization;

/// <summary>Well-known message format names used by MinimalKafka.</summary>
public static class MessageFormats
{
    /// <summary>The JSON message format.</summary>
    public const string Json = "json";

    /// <summary>The Avro message format.</summary>
    public const string Avro = "avro";

    /// <summary>The Protocol Buffers message format.</summary>
    public const string Protobuf = "protobuf";
}
