using MinimalKafka;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Qowaiv.DomainModel.Kafka.Features;

public interface IAggregateStore<TId, TAggregate>
    where TId : notnull
    where TAggregate : Aggregate<TAggregate, TId>, new()    
{
    Task<TAggregate> LoadAsync(TId id);
    Task SaveAsync(TAggregate aggregate);
}


internal class KafkaAggregateStore<TId, TAggregate>(IKafkaProducer producer)
    : IAggregateStore<TId, TAggregate>
    where TId : notnull
    where TAggregate : Aggregate<TAggregate, TId>, new()
{
    private ConcurrentDictionary<TId, List<KafkaEvent<TId>>> _streams = [];

    private static readonly Dictionary<string, Type> _eventTypes = typeof(TAggregate)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name == "When" && m.GetParameters().Length == 1)
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToDictionary(x=>x.Name, y => y);

    public Task<TAggregate> LoadAsync(TId id)
    {
        var events = _streams.GetOrAdd(id, _ => []);
        var buffer = EventBuffer.FromStorage(id, events, ToEvent);
        var aggregate = Aggregate.FromStorage<TAggregate, TId>(buffer);
        return Task.FromResult(aggregate);
    }

    private object ToEvent(KafkaEvent<TId> storedEvent)
    {
        return storedEvent.Payload.Deserialize(_eventTypes[storedEvent.Type], JsonSerializerOptions.Default)!;
    }

    internal Task SaveEvent(KafkaEvent<TId> @event)
    {
        var events = _streams.GetOrAdd(@event.StreamId, _ => []);
        events.Add(@event); 
        _streams.AddOrUpdate(@event.StreamId, events, (_, _) => events);
        return Task.CompletedTask;
    }

    public async Task SaveAsync(TAggregate aggregate)
    {
        if (!aggregate.Buffer.HasUncommitted)
        {
            return;
        }

        var events = aggregate.Buffer.Uncommitted
            .Select(x => ToKafkaEvent(x, aggregate.Id))
            .ToArray();

        foreach (var e in events)
        {
            await producer.ProduceAsync(typeof(TAggregate).Name, aggregate.Id, e);
        }   
    }

    private KafkaEvent<TId> ToKafkaEvent(object e, TId aggregateId)
    {
        var json = JsonSerializer.Serialize(e, e.GetType());
        return new() { 
            Payload = JsonDocument.Parse(json),
            StreamId = aggregateId,
            Type = e.GetType().Name 
        };
    }
}

internal class KafkaEvent<TId>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required TId StreamId { get; init; }
    public required string Type { get; init; }
    public required JsonDocument Payload { get; init; }
}