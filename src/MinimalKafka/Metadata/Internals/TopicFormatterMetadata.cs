namespace MinimalKafka.Metadata.Internals;

public interface ITopicFormatter
{
    string Format(string topicName);
}
