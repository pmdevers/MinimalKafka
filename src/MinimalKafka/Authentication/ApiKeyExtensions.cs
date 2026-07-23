namespace MinimalKafka.Authentication;

internal static class ApiKeyExtensions
{
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