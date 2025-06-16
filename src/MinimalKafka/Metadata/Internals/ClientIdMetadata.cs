using Confluent.Kafka;
using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class ClientIdMetadataAttribute(string clientId) : Attribute, IClientIdMetadata
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
