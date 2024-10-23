using Confluent.Kafka;
using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata;

public interface IClientIdMetadata : IConsumerConfigMetadata, IProducerConfigMetadata
{
    string ClientId { get; }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ClientIdMetadataAttribute(string clientId) : Attribute, IClientIdMetadata
{
    public string ClientId { get; } = clientId;

    public void Set(ClientConfig config)
    {
        config.ClientId = ClientId;
    }

    /// <inheritdoc/>
    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(ClientId), ClientId);
}
