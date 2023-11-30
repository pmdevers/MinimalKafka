namespace MinimalKafka.Factory;

public sealed class TopicDelegateResult
{
    public TopicDelegateResult(TopicDelegate topicDelegate, IReadOnlyList<object> metaData)
    {
        TopicDelegate = topicDelegate;
        TopicMetadata = metaData;
    }

    public TopicDelegate TopicDelegate { get; }

    public IReadOnlyList<object> TopicMetadata { get; }
}
