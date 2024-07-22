namespace Pmdevers.MinimalKafka.Builders;

public interface IKafkaBuilder
{
    IServiceProvider ServiceProvider { get; }
    KafkaDataSource? DataSource { get; set; }
    List<object> MetaData { get; }
}
