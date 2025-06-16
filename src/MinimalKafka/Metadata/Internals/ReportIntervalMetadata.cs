using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata.Internals;

internal class ReportIntervalMetadata(int reportInterval) : IReportIntervalMetadata
{
    public int ReportInterval { get; } = reportInterval;

    /// <inheritdoc/>
    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(ReportInterval), ReportInterval);
}
