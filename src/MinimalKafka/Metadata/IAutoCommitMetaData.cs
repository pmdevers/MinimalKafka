namespace MinimalKafka.Metadata;

public interface IAutoCommitMetaData : IConsumerConfigMetadata
{
    bool Enabled { get; }
}
