namespace MinimalKafka.Stream;

public record AggregateCommand<TKey>(TKey Id, string Name, object Payload);
