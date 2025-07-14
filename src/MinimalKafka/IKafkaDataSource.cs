namespace MinimalKafka;

/// <summary>
/// Represents a data source for managing Kafka topic delegates and processes.
/// </summary>
public interface IKafkaDataSource
{
    /// <summary>
    /// Adds a delegate handler for the specified topic and returns a convention builder for further configuration.
    /// </summary>
    /// <param name="topicName">The name of the Kafka topic.</param>
    /// <param name="handler">The delegate that will handle messages for the topic.</param>
    /// <returns>An <see cref="IKafkaConventionBuilder"/> for additional configuration.</returns>
    IKafkaConventionBuilder AddTopicDelegate(string topicName, Delegate handler);

    /// <summary>
    /// Gets the collection of Kafka processes managed by this data source.
    /// </summary>
    /// <returns>An enumerable of <see cref="IKafkaProcess"/> instances.</returns>
    IEnumerable<IKafkaProcess> GetProcesses();
}