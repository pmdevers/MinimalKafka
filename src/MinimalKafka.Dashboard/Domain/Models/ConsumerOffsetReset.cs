namespace MinimalKafka.Dashboard.Domain.Models;

internal sealed record ConsumerOffsetReset(string Topic, int Partition, long Offset);
