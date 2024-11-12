namespace MinimalKafka.Stream;

public record AggregateEvent<TKey>(TKey Id, string Name, object Payload);
