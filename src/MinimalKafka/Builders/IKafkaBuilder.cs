namespace MinimalKafka.Builders;

/// <summary>
/// Represents a builder for configuring and managing Kafka-related services, topics, and metadata.
/// </summary>
public interface IKafkaBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve dependencies for Kafka operations.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets or sets the <see cref="IKafkaDataSource"/> responsible for managing topic delegates and Kafka processes.
    /// </summary>
    IKafkaDataSource DataSource { get; set; }

    /// <summary>
    /// Gets the collection of metadata objects associated with the Kafka builder.
    /// </summary>
    List<object> MetaData { get; }
}
