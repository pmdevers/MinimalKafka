using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Builders;

/// <summary>
/// Provides a builder interface for configuring and registering MinimalKafka services.
/// Inherits conventions support from <see cref="IKafkaConventionBuilder"/>.
/// </summary>
public interface IAddKafkaBuilder : IKafkaConventionBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to register Kafka-related services.
    /// </summary>
    IServiceCollection Services { get; }
}
