namespace MinimalKafka.Serializers;

/// <summary>
/// Factory interface for creating Kafka serializers for different types.
/// </summary>
public interface ISerializerFactory
{
    /// <summary>
    /// Creates a serializer instance for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create a serializer for.</typeparam>
    /// <returns>A serializer instance for type T.</returns>
    IKafkaSerializer<T> Create<T>();
}
