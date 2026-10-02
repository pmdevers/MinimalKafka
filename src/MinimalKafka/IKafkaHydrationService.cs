namespace MinimalKafka;

/// <summary>
/// Handles dehydration before publishing and rehydration after deserialization for Kafka file payloads.
/// </summary>
public interface IKafkaHydrationService
{
    /// <summary>
    /// Stores file payload data and prepares the object graph for publishing.
    /// </summary>
    /// <param name="obj">The message payload to dehydrate.</param>
    Task DeHydrateAsync(object? obj);

    /// <summary>
    /// Loads file payload data back into the object graph after deserialization.
    /// </summary>
    /// <param name="obj">The message payload to rehydrate.</param>
    Task ReHydrateAsync(object? obj);
}
