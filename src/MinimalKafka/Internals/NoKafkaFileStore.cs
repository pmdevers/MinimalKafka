using Microsoft.Extensions.Logging;
using MinimalKafka.Helpers;

namespace MinimalKafka.Internals;

internal class NoKafkaFileStore(ILogger<NoKafkaFileStore> logger) : IKafkaFileStore
{
    private readonly ILogger<NoKafkaFileStore> _logger = logger;

    public Task<ReadOnlyMemory<byte>> LoadAsync(Guid key)
    {
        _logger.NoKafkaFileStoreLoad(key);
        return Task.FromResult(ReadOnlyMemory<byte>.Empty);
    }

    public Task StoreAsync(Guid key, ReadOnlyMemory<byte> data)
    {
        _logger.NoKafkaFileStoreStore(key);
        return Task.CompletedTask;
    }
}
