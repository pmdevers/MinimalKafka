using Confluent.Kafka;

namespace MinimalKafka.Internals;

internal class KafkaContextProducer(IProducer<byte[], byte[]> producer) : IKafkaProducer
{
    public async Task ProduceAsync(KafkaContext context, CancellationToken cancellationToken)
    {
        if(!context.Messages.Any())
            return;

        try
        {
            producer.InitTransactions(TimeSpan.FromSeconds(5));
            producer.BeginTransaction();
            foreach (var produce in context.Messages)
            {
                await producer.ProduceAsync(produce.Topic, new()
                {
                    Key = produce.Key,
                    Value = produce.Value,
                    Headers = produce.GetKafkaHeaders(),
                    Timestamp = produce.Timestamp,
                }, cancellationToken);
            }

            producer.CommitTransaction();
        }
        catch (ProduceException<byte[], byte[]> ex)
        {
            producer.AbortTransaction();
            throw new KafkaProcesException(ex, ex.Message);
        }
    }
}

    