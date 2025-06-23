namespace MinimalKafka.Metadata;

/// <summary>
/// Represents metadata for providing a serializer type for Kafka producer keys or values.
/// </summary>
public interface ISerializerMetadata
{
    /// <summary>
    /// Gets the serializer <see cref="Type"/> for the specified generic type parameter.
    /// </summary>
    /// <typeparam name="T">The type for which the serializer is requested.</typeparam>
    /// <returns>The <see cref="Type"/> of the serializer for <typeparamref name="T"/>.</returns>
    Type GetSerializerType<T>();
}
