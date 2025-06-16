namespace MinimalKafka.Metadata;
public interface IDeserializerMetadata
{
    public Func<IKafkaConsumerBuilder, object> Deserializer { get; }
}
