namespace MinimalKafka.Metadata;

public interface IClientIdMetadata : IConsumerConfigMetadata, IProducerConfigMetadata
{
    string ClientId { get; }
}
