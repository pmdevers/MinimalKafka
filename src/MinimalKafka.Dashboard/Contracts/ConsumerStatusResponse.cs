namespace MinimalKafka.Dashboard.Contracts;

/// <summary>Represents the runtime state of Kafka consumers.</summary>
/// <param name="IsRunning">True when consumers are running; otherwise false.</param>
public sealed record ConsumerStatusResponse(bool IsRunning);
