namespace MinimalKafka.Metadata;

/// <summary>
/// Represents metadata for specifying the Kafka bootstrap servers configuration.
/// </summary>
public interface IBootstrapServersMetadata
{
    /// <summary>
    /// Gets the bootstrap servers connection string used to connect to the Kafka cluster.
    /// </summary>
    string BootstrapServers { get; }
}
