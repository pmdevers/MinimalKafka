using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalKafka.Builders;
using MinimalKafka.Serializers;
using MinimalKafka.Stream;
using System.Diagnostics;

namespace MinimalKafka.Tests;

public class ServiceCollectionTests
{
    [Fact]
    public void AddMinimalKafka_ShouldAddKafkaServicesToServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        static void config(IKafkaConventionBuilder builder) 
        {
            Debug.Write("config called");
        }

        // Act
        services.AddMinimalKafka(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IKafkaBuilder>().Should().NotBeNull();
        serviceProvider.GetService(typeof(JsonTextSerializer<string>)).Should().NotBeNull();
        serviceProvider.GetService<IHostedService>().Should().BeOfType<KafkaService>();
    }

    [Fact]
    public void AddMinimalKafka_ShouldInvokeConfigurationActions()
    {
        // Arrange
        var services = new ServiceCollection();
        var invoked = false;
        void config(IKafkaConventionBuilder builder) => invoked = true;

        // Act
        services.AddMinimalKafka(config);

        // Assert
        invoked.Should().BeTrue();
    }

    [Fact]
    public void AddMinimalKafka_ShouldAddTransientJsonTextSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        static void config(IKafkaConventionBuilder builder)
        {
            Debug.Write("config called");
        }

        // Act
        services.AddMinimalKafka(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService(typeof(JsonTextSerializer<string>)).Should().NotBeNull();
    }

    [Fact]
    public void AddMinimalKafka_ShouldConfigureKafkaBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
                
        static void config(IAddKafkaBuilder builder) => 
            builder.Should().BeOfType<AddKafkaBuilder>();

        // Act
        services.AddMinimalKafka(config);
        var serviceProvider = services.BuildServiceProvider();
        var kafkaBuilder = serviceProvider.GetService<IKafkaBuilder>();

        // Assert
        kafkaBuilder.Should().NotBeNull();
    }

    [Fact]
    public void AddMinimalKafka_WithStreamStore_Should_Register()
    {
        // Arrange
        var services = new ServiceCollection();

        static void config(IAddKafkaBuilder builder) =>
            builder.WithStreamStore(typeof(InMemoryStore<,>));

        // Act
        services.AddMinimalKafka(config);
        var serviceProvider = services.BuildServiceProvider();
        var kafkaBuilder = serviceProvider.GetService<IStreamStore<Guid, string>>();

        // Assert
        kafkaBuilder.Should().NotBeNull();
    }
}