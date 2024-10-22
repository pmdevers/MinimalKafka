
using Confluent.Kafka;
using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata;
public interface ISerializerMetadata
{
    Type GetSerializerType<T>();
}

public class KeySerializerMetadata(Type serializerType) : ISerializerMetadata
{
    public Type GetSerializerType<T>()
    {
        if (serializerType.IsGenericType)
        {
            return serializerType.MakeGenericType(typeof(T));
        }
        else
        {
             return typeof(T);
        }
    }

    public override string ToString()
       => DebuggerHelpers.GetDebugText(nameof(KeySerializerMetadata), serializerType);
}

public class ValueSerializerMetadata(Type serializerType) : ISerializerMetadata
{
    public Type GetSerializerType<T>()
    {
        if (serializerType.IsGenericType)
        {
            return serializerType.MakeGenericType(typeof(T));
        }
        else
        {
            return typeof(T);
        }
    }
    public override string ToString()
       => DebuggerHelpers.GetDebugText(nameof(ValueSerializerMetadata), serializerType);
}
