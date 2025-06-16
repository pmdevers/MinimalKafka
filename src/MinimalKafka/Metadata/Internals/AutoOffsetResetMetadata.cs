using Confluent.Kafka;
using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata.Internals;

internal class AutoOffsetResetMetadata(AutoOffsetReset autoOffsetReset) : IAutoOffsetResetMetadata, IConsumerConfigMetadata
{
    public AutoOffsetReset AutoOffsetReset => autoOffsetReset;

    public void Set(ClientConfig config)
    {
        ((ConsumerConfig)config).AutoOffsetReset = autoOffsetReset;
    }

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(AutoOffsetReset), AutoOffsetReset.ToString());
}
