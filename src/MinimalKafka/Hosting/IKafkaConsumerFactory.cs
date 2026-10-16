namespace MinimalKafka.Hosting;

internal interface IKafkaConsumerFactory
{
    IKafkaConsumer Create();
}
