namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class TopicFormatterMetadataAttribute(Func<string, string>? formatter = null)
    : Attribute, ITopicFormatterMetadata
{
    public string Format(string topicName) => formatter?.Invoke(topicName) ?? topicName;

    public static TopicFormatterMetadataAttribute Default => new();
}