using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace MinimalKafka.Metadata.Internals;

public class ConfigurationMetadata : IConfigurationMetadata
{
    /// <summary>
    /// 
    /// </summary>
    public required IDictionary<string, string> Configuration { get; init; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="section"></param>
    /// <returns></returns>
    public static ConfigurationMetadata FromConfig(IConfiguration configuration)
    {
        var value = configuration.AsEnumerable(true)
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value ?? string.Empty))
            .ToDictionary();

        return new ConfigurationMetadata { Configuration = value };
    }
}
