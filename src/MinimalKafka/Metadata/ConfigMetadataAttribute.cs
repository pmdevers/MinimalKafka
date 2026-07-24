using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace MinimalKafka.Metadata;

/// <summary>
/// Represents a metadata attribute that holds Kafka configuration settings for methods. This attribute can be applied to methods to specify Kafka configuration values, which can be used to build consumer and producer configurations.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ConfigMetadataAttribute : Attribute
{
    private ConfigMetadataAttribute(Dictionary<string, string> config)
    {
        _config = config;
    }

    private readonly Dictionary<string, string> _config;
    /// <summary>
    /// Creates a ConfigMetadataAttribute instance from the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration object to extract settings from.</param>
    /// <returns>A new ConfigMetadataAttribute instance with the extracted configuration values.</returns>
    public void LoadFromConfig(IConfiguration configuration)
    {
        foreach (var kv in configuration.AsEnumerable(true))
        {
            _config[kv.Key] = kv.Value ?? string.Empty;
        }
    }

    /// <summary>
    /// Adds or updates a configuration key-value pair in the internal configuration dictionary.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    public void AddOrUpdate(string key, string value)
        => _config[key] = value;

    /// <summary>
    /// Builds and returns a <see cref="ConsumerConfig"/> instance based on the internal configuration dictionary. This method creates a new ConsumerConfig object using the stored configuration values.
    /// </summary>
    /// <returns>A new <see cref="ConsumerConfig"/> instance.</returns>
    public ConsumerConfig BuildConsumerConfig()
        => new(_config);

    /// <summary>
    /// Builds and returns a <see cref="ProducerConfig"/> instance based on the internal configuration dictionary. This method creates a new ProducerConfig object using the stored configuration values.
    /// </summary>
    /// <returns>A new <see cref="ProducerConfig"/> instance.</returns>
    public ProducerConfig BuildProducerConfig()
        => new(_config);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configMetadata"></param>
    /// <returns></returns>
    public static ConfigMetadataAttribute From(ConfigMetadataAttribute? configMetadata)
        => new(configMetadata?._config.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, string>());
}
