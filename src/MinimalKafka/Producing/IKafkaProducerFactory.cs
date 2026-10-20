using Confluent.Kafka;

namespace MinimalKafka.Producing;

internal interface IKafkaProducerFactory
{
    IProducer<byte[], byte[]> Create();
}
