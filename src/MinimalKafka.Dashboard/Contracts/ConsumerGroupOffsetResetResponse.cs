namespace MinimalKafka.Dashboard.Contracts;

internal sealed record ConsumerGroupOffsetResetResponse(
    string GroupId,
    int UpdatedCount);
