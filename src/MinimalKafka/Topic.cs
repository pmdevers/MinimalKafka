namespace MinimalKafka;

public class Topic
{
    public Topic(
        Delegate topicDelegate,
        TopicPattern topicPattern,
        int order,
        TopicMetadataCollection? metadata,
        string displayName
        )
    {

    }

    public Delegate TopicDelegate { get; }
    public int Order { get; }
    public TopicPattern TopicPattern { get; }

    public TopicMetadataCollection MetaData { get; }
}
