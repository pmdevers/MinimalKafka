
using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata;
public interface ISerializerMetadata
{
    Func<IKafkaProducerBuilder, object> SerializerType { get; }
}

public class KeySerializerMetadata(Func<IKafkaProducerBuilder, object> keySerializerType) : ISerializerMetadata
{
    public Func<IKafkaProducerBuilder, object> SerializerType { get; } = keySerializerType;

    public override string ToString()
       => DebuggerHelpers.GetDebugText(nameof(KeySerializerMetadata), SerializerType);
}

public class ValueSerializerMetadata(Func<IKafkaProducerBuilder, object> valueSerializerType) : ISerializerMetadata
{
    public Func<IKafkaProducerBuilder, object> SerializerType { get; } = valueSerializerType;

    public override string ToString()
       => DebuggerHelpers.GetDebugText(nameof(ValueSerializerMetadata), SerializerType);
}
