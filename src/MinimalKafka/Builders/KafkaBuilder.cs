
using MinimalKafka.Internals;

namespace MinimalKafka.Builders;
internal class KafkaBuilder(IServiceProvider serviceProvider) : IKafkaBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IKafkaDataSource? DataSource { get; set; }

    public List<object> MetaData { get; } = [];
}
