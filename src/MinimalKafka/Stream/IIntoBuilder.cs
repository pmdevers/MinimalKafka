namespace MinimalKafka.Stream;

public interface IIntoBuilder<TKey, TValue>
{
    IWithMetadataBuilder Into(Func<KafkaContext, TKey, TValue, Task> handler);
}

