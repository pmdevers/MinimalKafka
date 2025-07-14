namespace MinimalKafka.Metadata.Internals;


[AttributeUsage(AttributeTargets.Method)]
internal class ReportIntervalMetadataAttribute(int interval) : Attribute, IReportIntervalMetadata
{
    public int ReportInterval => interval;
}
