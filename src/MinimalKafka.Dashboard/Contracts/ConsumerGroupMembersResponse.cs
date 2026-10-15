namespace MinimalKafka.Dashboard.Contracts;

internal sealed record ConsumerGroupMembersResponse(
    string GroupId,
    IReadOnlyList<ConsumerGroupMemberDto> Members);
