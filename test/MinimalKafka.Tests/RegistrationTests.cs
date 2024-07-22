using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pmdevers.MinimalKafka;
using Pmdevers.MinimalKafka.Builders;

namespace MinimalKafka.Tests;

public class ServiceCollectionTests
{
    [Fact]
    public void AddMinimalKafka_should_register_services()
    {
        var services = new ServiceCollection();

        services.AddMinimalKafka(config =>
        {

        });

        var provider = services.BuildServiceProvider();

        var getBuilder = () => provider.GetRequiredService<IKafkaBuilder>();
        var getService = () => provider.GetService<IHostedService>() as KafkaService;

        getBuilder.Should().NotThrow();
        getService.Should().NotThrow();
    }
}