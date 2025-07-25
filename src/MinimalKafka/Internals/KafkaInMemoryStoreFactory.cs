﻿using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MinimalKafka.Internals;

internal class KafkaInMemoryStoreFactory(IServiceProvider serviceProvider) : BackgroundService, IKafkaStoreFactory
{
    private readonly ConcurrentDictionary<string, KafkaInMemoryStore> _stores = [];
    private readonly object _lock = new();

    public IKafkaStore GetStore(string topicName)
    {
        lock (_lock)
        {
            if (!_stores.TryGetValue(topicName, out KafkaInMemoryStore? store))
            {
                store = new KafkaInMemoryStore(serviceProvider);
                _stores.TryAdd(topicName, store);

            }
            return store;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            foreach(var store in _stores.Values) 
            { 
                store.CleanUp();
            }
        }
    }
}


internal class KafkaInMemoryStore(IServiceProvider serviceProvider) : IKafkaStore
{
    private readonly TimedConcurrentDictionary<byte[], byte[]> _store =
        new(TimeSpan.FromDays(7));

    public IServiceProvider ServiceProvider => serviceProvider;

    public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        byte[] localVal = value.ToArray();

        return _store.AddOrUpdate(key.ToArray(), 
            (k) => localVal, 
            (k, v) => localVal);
    }

    public void CleanUp()
    {
        _store.CleanUp();
    }

    public ValueTask<byte[]?> FindByIdAsync(ReadOnlySpan<byte> key)
    {
        return _store.FindByIdAsync(key.ToArray());
    }

    public IAsyncEnumerable<byte[]> GetItems()
    {
        return _store.GetItems().Values.ToAsyncEnumerable();
    }
}