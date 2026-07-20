using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class ConfigMetadataAttribute() : Attribute, IConfigMetadata
{
    private readonly Dictionary<string, string> _config = [];
    /// <summary>
    /// Creates a ConfigMetadataAttribute instance from the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration object to extract settings from.</param>
    /// <returns>A new ConfigMetadataAttribute instance with the extracted configuration values.</returns>
    public void LoadFromConfig(IConfiguration configuration)
    {
        foreach(var kv in configuration.AsEnumerable(true))
        {
            _config[kv.Key] = kv.Value ?? string.Empty;
        }
    }

    public void AddOrUpdate(string key, string value)
        => _config[key] = value;

    public ConsumerConfig BuildConsumerConfig()
        => new(_config);

    public ProducerConfig BuildProducerConfig() 
        => new(_config);
}
