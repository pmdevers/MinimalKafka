namespace MinimalKafka.Serializers;

internal class KafkaSerializerProxy<T> : IKafkaSerializer<T>
{
    private readonly IKafkaSerializer<T> _serializer;

    public KafkaSerializerProxy(ISerializerFactory factory)
    {
        _serializer = factory.Create<T>();
    }

    public T Deserialize(ReadOnlySpan<byte> value)
        => _serializer.Deserialize(value);

    public byte[] Serialize(T value)
        => _serializer.Serialize(value);
}