namespace MinimalKafka.Authentication;

/// <summary>
/// Provides extension methods for configuring API key authentication for Kafka.
/// </summary>
public static class ApiKeyExtensions
{
    /// <summary>
    /// Configures API key authentication for Kafka.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the Kafka builder.</typeparam>
    /// <param name="builder">The Kafka builder.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="apiSecret">The API secret.</param>
    /// <returns>The updated Kafka builder.</returns>
    public static TBuilder WithApiKey<TBuilder>(this TBuilder builder, string apiKey, string apiSecret)
        where TBuilder : IKafkaConfigBuilder
    {
        builder.UpdateConfig(b =>
        {
            b.AddOrUpdate("security.protocol", "SASL_SSL");
            b.AddOrUpdate("sasl.mechanism", "PLAIN");
            b.AddOrUpdate("sasl.username", apiKey);
            b.AddOrUpdate("sasl.password", apiSecret);
        });
        return builder;
    }
}