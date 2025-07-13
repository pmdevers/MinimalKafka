namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal class TopicFormatterMetadataAttribute(Func<string, string> formatter) : Attribute, ITopicFormaterMetadata
{
    public Func<string, string> TopicFormatter { get; } = formatter;
}
