namespace MinimalKafka.Dashboard.Contracts;

internal sealed record ConsumerGroupOffsetResetRequest(
    IReadOnlyList<ConsumerGroupOffsetResetItemDto> Offsets);
