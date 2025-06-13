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
        Level = LogLevel.Critical,
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
        Message = "Consumer returned an Exception!. {message}"
    )]
    public static partial void UnknownProcessException(this ILogger logger, string message);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' already closed."
        )]
    public static partial void ConsumerAlreadyClosed(this ILogger logger, string groupId, string clientId);

    [LoggerMessage(
       EventId = 9,
       Level = LogLevel.Information,
       Message = "Consumer with GroupId: '{GroupId}' and ClientId: '{ClientId}' committing offset."
       )]
    public static partial void Committing(this ILogger logger, string groupId, string clientId);
}
