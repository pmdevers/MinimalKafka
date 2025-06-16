namespace MinimalKafka.Metadata;

/// <summary>
/// Represents metadata for providing arbitrary configuration settings as key-value pairs.
/// </summary>
public interface IConfigurationMetadata
{
    /// <summary>
    /// Gets a dictionary containing configuration settings to be applied, where each entry represents a configuration key and its value.
    /// </summary>
    IDictionary<string, string> Configuration { get; }
}
