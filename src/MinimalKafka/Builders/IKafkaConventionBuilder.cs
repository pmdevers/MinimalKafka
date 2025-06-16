namespace MinimalKafka.Builders;

/// <summary>
/// Defines a builder interface for registering and composing Kafka conventions.
/// </summary>
public interface IKafkaConventionBuilder
{
    /// <summary>
    /// Adds a convention to be applied to the <see cref="IKafkaBuilder"/> during configuration.
    /// </summary>
    /// <param name="convention">An action that configures the <see cref="IKafkaBuilder"/>.</param>
    void Add(Action<IKafkaBuilder> convention);

    /// <summary>
    /// Adds a final convention to be applied to the <see cref="IKafkaBuilder"/> after all other conventions.
    /// </summary>
    /// <param name="finalConvention">An action that performs final configuration on the <see cref="IKafkaBuilder"/>.</param>
    void Finally(Action<IKafkaBuilder> finalConvention);
}