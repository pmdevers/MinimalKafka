namespace MinimalKafka.Metadata;

/// <summary>
/// Specifies the reporting interval for a Kafka consumer or producer method. This attribute can be applied to methods to indicate how frequently reports should be generated or processed. 
/// </summary>
/// <param name="interval">The reporting interval in seconds.</param>
[AttributeUsage(AttributeTargets.Method)]
public class ReportIntervalMetadataAttribute(int interval) : Attribute
{
    /// <summary>
    /// Gets the reporting interval in seconds for the Kafka consumer or producer method. This value indicates how frequently reports should be generated or processed.
    /// </summary>
    public int ReportInterval => interval;
}
