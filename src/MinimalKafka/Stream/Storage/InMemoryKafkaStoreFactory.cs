using System.Collections.Concurrent;

namespace MinimalKafka.Stream;

internal sealed class InMemoryKafkaStoreFactory(IServiceProvider serviceProvider) : IKafkaStoreFactory
{
    private readonly ConcurrentDictionary<string, InMemoryKafkaStore> _stores = new(StringComparer.Ordinal);

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IKafkaStore GetStore(string topicName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        return _stores.GetOrAdd(topicName, name => new InMemoryKafkaStore(ServiceProvider, name));
    }

    public void Dispose()
    {
    }

    private sealed class InMemoryKafkaStore(IServiceProvider serviceProvider, string topicName) : IKafkaStore
    {
        private readonly ConcurrentDictionary<string, byte[]> _items = new(StringComparer.Ordinal);

        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public string TopicName { get; } = topicName;

        public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
        {
            var serializedKey = Convert.ToBase64String(key);
            var serializedValue = value.ToArray();
            _items[serializedKey] = serializedValue;
            return ValueTask.FromResult(serializedValue);
        }

        public ValueTask<byte[]?> FindByKeyAsync(ReadOnlySpan<byte> key)
        {
            var serializedKey = Convert.ToBase64String(key);
            return ValueTask.FromResult(_items.TryGetValue(serializedKey, out var value) ? value : null);
        }

#pragma warning disable CS1998
        public async IAsyncEnumerable<byte[]> GetItems()
        {
            foreach (var item in _items.Values)
            {
                yield return item;
            }
        }
#pragma warning restore CS1998
    }
}
