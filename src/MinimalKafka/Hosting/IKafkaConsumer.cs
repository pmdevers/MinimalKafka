using Confluent.Kafka;

namespace MinimalKafka.Hosting;

internal interface IKafkaConsumer : IDisposable
{
    void Subscribe(IEnumerable<string> topics);

    ConsumeResult<byte[], byte[]>? Consume(TimeSpan timeout);

    void Commit(ConsumeResult<byte[], byte[]> result);

    void Close();
}
