namespace MinimalKafka.Builders;
internal class KafkaConventionBuilder(
    ICollection<Action<IKafkaBuilder>> conventions,
    ICollection<Action<IKafkaBuilder>> finallyConventions) : IKafkaConventionBuilder
{
    public void Add(Action<IKafkaBuilder> convention)
    {
        conventions.Add(convention);
    }

    public void Finally(Action<IKafkaBuilder> finalConvention)
    {
        finallyConventions.Add(finalConvention);
    }
}
