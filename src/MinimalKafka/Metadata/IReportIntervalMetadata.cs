using Pmdevers.MinimalKafka.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
