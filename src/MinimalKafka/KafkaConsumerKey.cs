namespace MinimalKafka;

/// <summary>
/// Describes a unique key for a consumer.
/// </summary>
public sealed record KafkaConsumerKey()
{
    /// <summary>
    /// Create a random key for a given topic.
    /// </summary>
    public static KafkaConsumerKey Random(string topicName)
        => new()
        {
            TopicName = topicName,
            GroupId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString(),
        };

    /// <summary>
    /// The name of the topic.
    /// </summary>
    public required string TopicName { get; init; }

    /// <summary>
    /// The consumer group id.
    /// </summary>
    public required string GroupId { get; init; }

    /// <summary>
    /// The client id.
    /// </summary>
    public required string ClientId { get; init; }
}

