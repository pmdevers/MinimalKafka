using Confluent.Kafka;

namespace MinimalKafka.Producing;

public interface IMessageProducer
{
    Task<DeliveryResult<string, byte[]>> ProduceAsync<TValue>(
        string topic,
        TValue value,
        string? key = null,
        Headers? headers = null,
        CancellationToken cancellationToken = default,
        string? format = null);
}




