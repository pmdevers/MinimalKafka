using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata.Internals;

internal class KeyDeserializerMetadata(Func<IKafkaConsumerBuilder, object> keyDeserializerType) : IDeserializerMetadata
{
    public Func<IKafkaConsumerBuilder, object> Deserializer => keyDeserializerType;

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(KeyDeserializerMetadata), Deserializer);
}
