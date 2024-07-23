using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata;

public interface IDeserializerMetadata
{
    public Func<IKafkaConsumerBuilder, object> Deserializer { get; }
}

public class KeyDeserializerMetadata(Func<IKafkaConsumerBuilder, object> keyDeserializerType) : IDeserializerMetadata
{
    public Func<IKafkaConsumerBuilder, object> Deserializer => keyDeserializerType;

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(KeyDeserializerMetadata), Deserializer);
}

public class ValueDeserializerMetadata(Func<IKafkaConsumerBuilder, object> valueDeserializerType) : IDeserializerMetadata
{
    public Func<IKafkaConsumerBuilder, object> Deserializer => valueDeserializerType;

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(ValueDeserializerMetadata), Deserializer);
}
