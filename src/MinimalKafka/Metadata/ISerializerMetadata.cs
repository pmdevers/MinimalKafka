namespace MinimalKafka.Metadata;
public interface ISerializerMetadata
{
    Type GetSerializerType<T>();
}
