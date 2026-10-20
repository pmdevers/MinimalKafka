namespace MinimalKafka.Dashboard.Domain.Models;

internal sealed record TopicMessage(string Topic, int Partition, long Offset, string Value, DateTimeOffset Timestamp);
