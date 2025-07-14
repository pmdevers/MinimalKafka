using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class ConfigMetadataAttribute(IDictionary<string, string> config) : Attribute, IConfigMetadata
{
    public ConsumerConfig ConsumerConfig { get;  init; } = new ConsumerConfig(config);
    public ProducerConfig ProducerConfig { get; init; } = new ProducerConfig(config);

    /// <summary>
    /// Creates a ConfigMetadataAttribute instance from the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration object to extract settings from.</param>
    /// <returns>A new ConfigMetadataAttribute instance with the extracted configuration values.</returns>
    public static ConfigMetadataAttribute FromConfig(IConfiguration configuration)
    {
        var value = configuration.AsEnumerable(true)
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value ?? string.Empty))
            .ToDictionary();

        return new(value);
    }
}
