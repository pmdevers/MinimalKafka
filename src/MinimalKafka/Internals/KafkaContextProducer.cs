using Confluent.Kafka;

namespace MinimalKafka.Internals;

internal class KafkaContextProducer : IKafkaProducer
{
    private readonly IProducer<byte[], byte[]> _producer;

    public KafkaContextProducer(IProducer<byte[], byte[]> producer)
    {
        _producer = producer;
        _producer.InitTransactions(TimeSpan.FromSeconds(5));
    }
    public async Task ProduceAsync(KafkaContext context, CancellationToken cancellationToken)
    {
        if(!context.Messages.Any())
            return;

        try
        {
            _producer.BeginTransaction();
            foreach (var produce in context.Messages)
            {
                await _producer.ProduceAsync(produce.Topic, new()
                {
                    Key = produce.Key,
                    Value = produce.Value,
                    Headers = produce.GetKafkaHeaders()
                }, cancellationToken);
            }

            _producer.CommitTransaction();
        }
        catch (ProduceException<byte[], byte[]> ex)
        {
            _producer.AbortTransaction();
            throw new KafkaProcesException(ex, ex.Message);
        }
    }
}

    