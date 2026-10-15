namespace MinimalKafka.Dashboard.Contracts;

internal sealed record ConsumerGroupOffsetsResponse(
    string GroupId,
    IReadOnlyList<ConsumerGroupTopicOffsetDto> Offsets);
