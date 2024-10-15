namespace MinimalKafka.Stream;

public interface IIntoBuilder<TKey, TValue>
{
    void Into(Func<KafkaContext, TKey, TValue, Task> handler);
}

