namespace MinimalKafka.Dashboard.Domain.Models;

internal sealed record OffsetInfo(string Topic, int Partition, long? CommittedOffset, long? HighOffset, long? Lag);
