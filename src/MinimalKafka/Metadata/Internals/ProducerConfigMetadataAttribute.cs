using Confluent.Kafka;

namespace MinimalKafka.Metadata.Internals;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal class ProducerConfigMetadataAttribute : Attribute, IProducerConfigMetadata
{
    public ProducerConfig Config { get; } = new ProducerConfig();
}