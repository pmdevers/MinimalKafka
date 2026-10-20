namespace MinimalKafka.Dashboard.Domain.Models;

internal sealed record Member(
    string MemberId,
    string ClientId,
    string? GroupInstanceId,
    string ClientHost,
    IReadOnlyList<string> AssignedPartitions);
