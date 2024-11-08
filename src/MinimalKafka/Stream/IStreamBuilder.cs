using MinimalKafka.Builders;

namespace MinimalKafka.Stream;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface IStreamBuilder<K1, V1> : IKafkaConventionBuilder, IIntoBuilder<K1, V1>
#pragma warning restore S2326 // Unused type parameters should be removed
{
    IJoinBuilder<V1, V2> Join<K2, V2>(string topic);
}

