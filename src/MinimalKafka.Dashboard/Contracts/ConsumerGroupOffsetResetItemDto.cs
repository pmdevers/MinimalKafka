namespace MinimalKafka.Dashboard.Contracts;

internal sealed record ConsumerGroupOffsetResetItemDto(
    string Topic,
    int Partition,
    long Offset);
