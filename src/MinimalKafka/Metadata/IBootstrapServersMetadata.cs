using Confluent.Kafka;
using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata;
public interface IBootstrapServersMetadata
{
    public string BootstrapServers { get; }
}

public class BootstrapServersMetadata(string bootstrapServers) : IBootstrapServersMetadata, IConsumerConfigMetadata, IProducerConfigMetadata
{
    public string BootstrapServers => bootstrapServers;

    public void Set(ClientConfig config)
    {
        config.BootstrapServers = BootstrapServers;
    }

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(BootstrapServers), BootstrapServers);
}
