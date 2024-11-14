namespace MinimalKafka.Stream;

public interface IAggregate<in TKey, TSelf> : IEquatable<TSelf>
{
    abstract static TSelf Create(KafkaContext context, TKey key);
}

public interface ICommand<TAggregate>
{
    TAggregate Execute(TAggregate aggregate);
}