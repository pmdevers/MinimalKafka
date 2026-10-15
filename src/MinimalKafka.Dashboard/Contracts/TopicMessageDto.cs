namespace MinimalKafka.Dashboard.Contracts;

internal sealed record TopicMessageDto(
    string Topic,
    int Partition,
    long Offset,
    string Value,
    DateTimeOffset Timestamp);
