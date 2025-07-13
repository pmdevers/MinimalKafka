namespace MinimalKafka.Internals;

internal class KafkaConsumerStore : IKafkaConsumerStore
{
    public Task AddOrUpdate(byte[] key, byte[] value)
    {
        return Task.CompletedTask;
    }

    public Task<byte[]> FindByKey(byte[] key)
    {
        return Task.FromResult(Array.Empty<byte>());
    }
}
