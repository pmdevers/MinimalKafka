namespace MinimalKafka.Dashboard.Contracts;

internal sealed record TopicMessagesResponse(
    string Topic,
    int RequestedCount,
    IReadOnlyList<TopicMessageDto> Messages);
