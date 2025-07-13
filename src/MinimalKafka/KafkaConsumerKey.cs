namespace MinimalKafka;

/// <summary>
/// 
/// </summary>
/// <param name="TopicName"></param>
/// <param name="GroupId"></param>
/// <param name="ClientId"></param>
public record KafkaConsumerKey(string TopicName, string GroupId, string ClientId)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topicName"></param>
    /// <returns></returns>
    public static KafkaConsumerKey Random(string topicName)
        => new(topicName, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
};

    