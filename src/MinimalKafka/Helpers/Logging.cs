using Microsoft.Extensions.Logging;

namespace MinimalKafka.Helpers;

internal static partial class Logging
{

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}', Consumed '{Records}' records from topic '{Topic}' so far."
    )]
    public static partial void RecordsConsumed(this ILogger logger, string groupId, string clientId, long records, string topic);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' was closed, Operation cancelled."
        )]
    public static partial void OperatonCanceled(this ILogger logger, string groupId, string clientId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' was closed."
        )]
    public static partial void ConsumerClosed(this ILogger logger, string groupId, string clientId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' Subscribed to topic: '{Topic}'."
        )]
    public static partial void Subscribed(this ILogger logger, string groupId, string clientId, string topic);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Consumer {MemberId} had partitions {Partitions} revoked"
        )]
    public static partial void PartitionsRevoked(this ILogger logger, string memberid, string partitions);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Consumer returned an Empty Context!."
        )]
    public static partial void EmptyContext(this ILogger logger);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Critical,
        Message = "Dropping out of consume loop."
        )]
    public static partial void DropOutOfConsumeLoop(this ILogger logger);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Consumer for topic: '{topic}' returned an Exception!. {message}"
    )]
    public static partial void UnknownProcessException(this ILogger logger, string topic, string message);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' already closed."
        )]
    public static partial void ConsumerAlreadyClosed(this ILogger logger, string groupId, string clientId);

    [LoggerMessage(
       EventId = 9,
       Level = LogLevel.Information,
       Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' committing offset for topic: '{Topic}', partition: '{Partition}', offset: '{Offset}'."
       )]
    public static partial void Committing(this ILogger logger, string groupId, string clientId, string topic, int partition, long offset);

    [LoggerMessage(
       EventId = 10,
       Level = LogLevel.Information,
       Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' no offset stored for topic: {Topic}"
       )]
    public static partial void NoOffsetStored(this ILogger logger, string groupId, string clientId, string topic);

    [LoggerMessage(
       EventId = 11,
       Level = LogLevel.Critical,
       Message = "Error while producing to topic: '{Topic}'. {Message}")]
    public static partial void ErrorWhileProducing(this ILogger logger, string topic, string message);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Start consume topic: '{Topic}' with GroupId {groupId}.")]
    public static partial void StartConsume(this ILogger logger, string topic, string groupId);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Information,
        Message = "Finished consume topic: '{Topic}' with GroupId '{groupId}' after '{elapsed}'.")]
    public static partial void FinishedConsume(this ILogger logger, string topic, string groupId, TimeSpan elapsed);

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Information,
        Message = "Committed offsets for topic: '{Topic}' with GroupId '{groupId}' and ClientId '{clientId}'.")]
    public static partial void Committed(this ILogger logger, string groupId, string clientId, string topic);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Error,
        Message = "Failed to commit offsets for topic: '{Topic}' with GroupId '{groupId}' and ClientId '{clientId}'. {Message}")]
    public static partial void CommitFailed(this ILogger logger, string groupId, string clientId, string topic, string message);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Warning,
        Message = "Queued message to DLQ topic '{DlqTopic}' from source topic '{SourceTopic}', partition '{Partition}', offset '{Offset}', resolution key '{ResolutionKey}'.")]
    public static partial void DeadLetterQueued(this ILogger logger, string dlqTopic, string sourceTopic, int partition, long offset, string resolutionKey);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Information,
        Message = "Auto-resolved DLQ item for source topic '{SourceTopic}', partition '{Partition}', offset '{Offset}'.")]
    public static partial void DeadLetterAutoResolved(this ILogger logger, string sourceTopic, int partition, long offset);

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Warning,
        Message = "Waiting for DLQ resolution before commit for source topic '{SourceTopic}', partition '{Partition}', offset '{Offset}'.")]
    public static partial void WaitingForDeadLetterResolution(this ILogger logger, string sourceTopic, int partition, long offset);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Information,
        Message = "DLQ item resolved; resuming commit for source topic '{SourceTopic}', partition '{Partition}', offset '{Offset}'.")]
    public static partial void DeadLetterResolved(this ILogger logger, string sourceTopic, int partition, long offset);
}
