using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class ConfigMetadataAttribute(IDictionary<string, string> config) : Attribute, IConfigMetadata
{
    public ConsumerConfig ConsumerConfig { get; private set; } = new ConsumerConfig(config);
    public ProducerConfig ProducerConfig { get; private set; } = new ProducerConfig(config);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static ConfigMetadataAttribute FromConfig(IConfiguration configuration)
    {
        var value = configuration.AsEnumerable(true)
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value ?? string.Empty))
            .ToDictionary();

        return new(value);
    }
}
