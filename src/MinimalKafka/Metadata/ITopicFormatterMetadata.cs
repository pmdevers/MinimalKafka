namespace MinimalKafka.Metadata;

/// <summary>
/// Defines a contract for formatting Kafka topic names.
/// </summary>
public interface ITopicFormatterMetadata
{
    /// <summary>
    /// Formats the specified topic name according to custom logic.
    /// </summary>
    /// <param name="topicName">The original topic name.</param>
    /// <returns>The formatted topic name.</returns>
    string Format(string topicName);
}
