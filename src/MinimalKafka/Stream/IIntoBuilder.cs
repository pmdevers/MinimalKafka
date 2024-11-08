using MinimalKafka.Builders;

namespace MinimalKafka.Stream;

public interface IIntoBuilder<TValue>
{
    IKafkaConventionBuilder Into(Func<KafkaContext, TValue, Task> handler);
}

public interface IIntoBuilder<TKey, TValue>
{
    IKafkaConventionBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler);
}

