using Pmdevers.MinimalKafka;
using Pmdevers.MinimalKafka.Builders;

namespace MinimalKafka.Tests;
public class KafkaServiceTests
{
    [Fact]
    public void KafkaService_should_call_Builder()
    {
        var builder = Substitute.For<IKafkaBuilder>();
        var services = Substitute.For<IServiceProvider>();

        builder.DataSource = new KafkaDataSource(services);

        var service = new KafkaService(builder);

    }
}


