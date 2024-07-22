using Confluent.Kafka;
using Pmdevers.MinimalKafka.Helpers;

namespace Pmdevers.MinimalKafka.Metadata;
public interface IConsumerConfigMetadata
{
    public void Set(ConsumerConfig config);
}

public interface IAutoOffsetResetMetadata
{
    public AutoOffsetReset AutoOffsetReset { get; }
}

public class AutoOffsetResetMetadata(AutoOffsetReset autoOffsetReset) : IAutoOffsetResetMetadata, IConsumerConfigMetadata
{
    public AutoOffsetReset AutoOffsetReset => autoOffsetReset;

    public void Set(ConsumerConfig config)
    {
        config.AutoOffsetReset = AutoOffsetReset;
    }

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(AutoOffsetReset), AutoOffsetReset.ToString());
}
