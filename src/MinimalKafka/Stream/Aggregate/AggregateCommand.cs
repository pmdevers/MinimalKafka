namespace MinimalKafka.Stream;


public static class AggregateCommand 
{
    public static AggregateCommand<TKey> Create<TKey, TCommand>(TKey id, TCommand command)
        where TCommand : notnull
    {
        return new AggregateCommand<TKey>(id, typeof(TCommand).Name, command);
    }
}

public record AggregateCommand<TKey>(TKey Id, string Name, object Payload);
