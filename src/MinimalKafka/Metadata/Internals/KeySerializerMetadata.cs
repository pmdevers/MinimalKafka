using MinimalKafka.Helpers;

namespace MinimalKafka.Metadata.Internals;

internal class KeySerializerMetadata(Type serializerType) : ISerializerMetadata
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
