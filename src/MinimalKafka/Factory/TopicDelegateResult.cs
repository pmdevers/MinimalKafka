namespace MinimalKafka.Factory;

public sealed class TopicDelegateResult
{
    public TopicDelegateResult(TopicDelegate topicDelegate, IReadOnlyList<object> metaData, Type keyType, Type valueType)
    {
        TopicDelegate = topicDelegate;
        TopicMetadata = metaData;
        KeyType = keyType;
        ValueType = valueType;
    }

    public TopicDelegate TopicDelegate { get; }

    public Type KeyType { get; }
    public Type ValueType { get; }

    public IReadOnlyList<object> TopicMetadata { get; }
}
