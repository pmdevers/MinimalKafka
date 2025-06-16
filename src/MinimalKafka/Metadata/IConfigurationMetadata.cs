namespace MinimalKafka.Metadata;

public interface IConfigurationMetadata
{
    IDictionary<string, string> Configuration { get; }
}
