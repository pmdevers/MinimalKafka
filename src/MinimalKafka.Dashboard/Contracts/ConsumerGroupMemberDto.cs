namespace MinimalKafka.Dashboard.Contracts;

internal sealed record ConsumerGroupMemberDto(
    string MemberId,
    string ClientId,
    string? GroupInstanceId,
    string ClientHost,
    IReadOnlyList<string> AssignedPartitions);
