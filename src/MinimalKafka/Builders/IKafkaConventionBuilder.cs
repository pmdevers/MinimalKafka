namespace MinimalKafka.Builders;

public interface IKafkaConventionBuilder
{
    void Add(Action<IKafkaBuilder> convention);
    void Finally(Action<IKafkaBuilder> finalConvention);
}