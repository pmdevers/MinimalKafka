namespace MinimalKafka.Builders;

public interface IKafkaBuilder
{
    IServiceProvider ServiceProvider { get; }
    IKafkaDataSource DataSource { get; set; }
    List<object> MetaData { get; }
}
