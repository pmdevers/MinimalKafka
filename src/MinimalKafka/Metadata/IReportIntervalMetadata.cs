namespace MinimalKafka.Metadata;
/// <summary>
/// Represents metadata for specifying the report interval configuration for a Kafka client.
/// </summary>
public interface IReportIntervalMetadata
{
    /// <summary>
    /// Gets the report interval, typically in milliseconds, used for configuring how often reports or statistics are generated.
    /// </summary>
    int ReportInterval { get; }
}
