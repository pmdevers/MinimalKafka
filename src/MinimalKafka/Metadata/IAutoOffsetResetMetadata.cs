using Confluent.Kafka;

namespace MinimalKafka.Metadata;

public interface IClientConfigMetadata
{
    public void Set(ClientConfig config);
}

public interface IConsumerConfigMetadata : IClientConfigMetadata
{
}

public interface IProducerConfigMetadata : IClientConfigMetadata
{
}

public interface IAutoOffsetResetMetadata
{
    public AutoOffsetReset AutoOffsetReset { get; }
}
