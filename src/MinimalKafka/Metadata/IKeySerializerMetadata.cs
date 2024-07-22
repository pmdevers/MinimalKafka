using Confluent.Kafka;
using Pmdevers.MinimalKafka.Helpers;

namespace Pmdevers.MinimalKafka.Metadata;
public interface IKeyDeserializerMetadata
{
    public Func<IServiceProvider, Type, object> KeyDeserializer { get; }

}

public class KeyDeserializerMetaData(Func<IServiceProvider, Type, object> keyDeserializerType) : IKeyDeserializerMetadata
{
    public Func<IServiceProvider, Type, object> KeyDeserializer => keyDeserializerType;

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(KeyDeserializer), KeyDeserializer);
}


public interface IValueDeserializerMetadata
{
    public Func<IServiceProvider, Type, object> ValueDeserializer { get; }

}

public class ValueDeserializerMetaData(Func<IServiceProvider, Type, object> valueDeserializerType) : IValueDeserializerMetadata
{
    public Func<IServiceProvider, Type, object> ValueDeserializer => valueDeserializerType;

    public override string ToString()
        => DebuggerHelpers.GetDebugText(nameof(ValueDeserializer), ValueDeserializer);
}
