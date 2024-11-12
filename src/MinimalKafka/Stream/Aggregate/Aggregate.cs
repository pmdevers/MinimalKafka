namespace MinimalKafka.Stream;

public abstract record Aggregate<TKey>(TKey Id);