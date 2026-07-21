
using MinimalKafka.Middlewares;

namespace MinimalKafka.Builders;

internal class KafkaBuilder(IServiceProvider serviceProvider) : IKafkaBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IKafkaDataSource? DataSource { get; set; }

    public List<object> MetaData { get; } = [];

    public List<Func<IServiceProvider, KafkaMiddlewareDelegate>> Middlewares { get; } = [];
}
