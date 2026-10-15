namespace MinimalKafka.Dashboard.Contracts;

internal sealed record ConsumerGroupTopicOffsetDto(
    string Topic,
    int Partition,
    long? CommittedOffset,
    long? HighOffset,
    long? Lag);
