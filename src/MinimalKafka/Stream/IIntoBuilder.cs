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

public static class IIntoBuilderExtensions 
{
    public static IKafkaConventionBuilder Into<TKey, V1, V2>(this IIntoBuilder<TKey, (V1?, V2?)> builder, string topic)
        => builder.Into(async (c, k, v) =>
        {
            await c.ProduceAsync(topic, k, v);
        });
}