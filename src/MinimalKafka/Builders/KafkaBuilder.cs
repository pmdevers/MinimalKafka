namespace Pmdevers.MinimalKafka.Builders;

public class KafkaBuilder(IServiceProvider serviceProvider) : IKafkaBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public IKafkaDataSource DataSource { get; set; } = new KafkaDataSource(serviceProvider);
    public List<object> MetaData { get; } = [];
}
