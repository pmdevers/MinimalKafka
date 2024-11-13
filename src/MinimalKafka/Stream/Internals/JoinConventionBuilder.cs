using MinimalKafka.Builders;

namespace MinimalKafka.Stream.Internals;

internal class JoinConventionBuilder(
    IKafkaConventionBuilder left, 
    IKafkaConventionBuilder right) : IKafkaConventionBuilder
{
    public void Add(Action<IKafkaBuilder> convention)
    {
        left.Add(convention);
        right.Add(convention);
    }

    public void Finally(Action<IKafkaBuilder> finalConvention)
    {
        left.Finally(finalConvention);
        right.Finally(finalConvention);
    }
}
