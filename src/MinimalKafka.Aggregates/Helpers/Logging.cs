using Microsoft.Extensions.Logging;

namespace MinimalKafka.Aggregates.Helpers;
internal static partial class Logging
{
    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Information,
        Message = "Command {CommandType} with version {CommandVersion} does not match state version {StateVersion} for aggregate {AggregateType}"
    )]
    public static partial void VersionMismatch(this ILogger logger, string commandType, int commandVersion, int stateVersion, string aggregateType);


    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Information,
        Message = "Command {CommandType} with version {Version} did not produce a new state for aggregate {AggregateType}."
    )]
    public static partial void VersionNotChanged(this ILogger logger, string commandType, int version, string aggregateType);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Information,
        Message = "Command {CommandType} with version {Version} failed to execute successfully on aggregate {AggregateType}."
    )]
    public static partial void CommandError(this ILogger logger, string commandType, int version, string aggregateType);

    [LoggerMessage(
        eventId: 503,
        level: LogLevel.Information,
        message: "Command {CommandType} with version {Version} was applied successfully to aggregate {AggregateType}."
    )]
    public static partial void CommandApplied(this ILogger logger, string commandType, int version, string aggregateType);
}
