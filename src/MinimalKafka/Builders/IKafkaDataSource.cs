namespace MinimalKafka.Builders;

public interface IKafkaDataSource
{
    IServiceProvider ServiceProvider { get; }
    IKafkaConventionBuilder AddTopicDelegate(string topicName, Delegate handler);
    IEnumerable<IKafkaProcess> GetProceses();
}
