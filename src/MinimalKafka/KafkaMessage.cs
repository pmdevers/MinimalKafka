namespace MinimalKafka;

/// <summary>
/// Represents a Kafka message.
/// </summary>
public record KafkaMessage(string Topic, byte[] Key, byte[] Value, Dictionary<string, string> Headers)
{
    /// <summary>
    /// 
    /// </summary>
    public string Topic { get; init; } = Topic;
    /// <summary>
    /// 
    /// </summary>
    public byte[] Key { get; init; } = Key;
    /// <summary>
    /// 
    /// </summary>
    public byte[] Value { get; init; } = Value;
    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = Headers;

    /// <summary>
    /// 
    /// </summary>
    public int Partition { get; init; } = 0;

    /// <summary>
    /// 
    /// </summary>
    public long Offset { get; init; } = 0;

    /// <summary>
    /// 
    /// </summary>
    public static KafkaMessage Empty { get; } = new KafkaMessage(string.Empty, [], [], []);
};
