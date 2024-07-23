using Confluent.Kafka;
using MinimalKafka.Helpers;

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

public class AutoOffsetResetMetadata(AutoOffsetReset autoOffsetReset) : IAutoOffsetResetMetadata, IConsumerConfigMetadata
{
    public AutoOffsetReset AutoOffsetReset => autoOffsetReset;

    public void Set(ClientConfig config)
    {
        ((ConsumerConfig)config).AutoOffsetReset = AutoOffsetReset;
    }

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(AutoOffsetReset), AutoOffsetReset.ToString());
}
