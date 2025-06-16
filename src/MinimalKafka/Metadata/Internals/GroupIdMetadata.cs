using Confluent.Kafka;
using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata.Internals;

public class GroupIdMetadata(string name) : IGroupIdMetadata, IConsumerConfigMetadata
{
    public string GroupId { get; } = name;

    public void Set(ClientConfig config)
    {
        ((ConsumerConfig)config).GroupId = GroupId;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(GroupId), GroupId);
    }
}