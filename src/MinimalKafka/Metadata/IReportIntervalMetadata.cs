using Pmdevers.MinimalKafka.Helpers;

namespace Pmdevers.MinimalKafka.Metadata;
public interface IReportIntervalMetadata
{
    int ReportInterval { get; }
}

public class ReportIntervalMetaData(int reportInterval) : IReportIntervalMetadata
{
    public int ReportInterval { get; } = reportInterval;

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(ReportInterval), ReportInterval);
    }
}
