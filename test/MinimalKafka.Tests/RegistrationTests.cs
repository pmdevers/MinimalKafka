//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using MinimalKafka.Builders;
//using MinimalKafka.Builders.Internals;
//using MinimalKafka.Extension;
//using MinimalKafka.Metadata;
//using MinimalKafka.Serializers;
//using MinimalKafka.Stream;
//using MinimalKafka.Stream.Storage;
//using System.Diagnostics;

//namespace MinimalKafka.Tests;

//public class ServiceCollectionTests
//{
//    [Fact]
//    public void AddMinimalKafka_ShouldAddKafkaServicesToServiceCollection()
//    {
//        // Arrange
//        var services = new ServiceCollection();
//        static void config(IKafkaConventionBuilder builder) 
//        {
//            Debug.Write("config called");
//        }

//        // Act
//        services.AddMinimalKafka(config);
//        var serviceProvider = services.BuildServiceProvider();

//        // Assert
//        serviceProvider.GetService<IKafkaBuilder>().Should().NotBeNull();
//        serviceProvider.GetService(typeof(JsonTextSerializer<string>)).Should().NotBeNull();
//        serviceProvider.GetService<IHostedService>().Should().BeOfType<KafkaService>();
//    }

//    [Fact]
//    public void AddMinimalKafka_ShouldInvokeConfigurationActions()
//    {
//        // Arrange
//        var services = new ServiceCollection();
//        var invoked = false;
//        void config(IKafkaConventionBuilder builder) => invoked = true;

//        // Act
//        services.AddMinimalKafka(config);

//        // Assert
//        invoked.Should().BeTrue();
//    }

//    [Fact]
//    public void AddMinimalKafka_ShouldAddTransientJsonTextSerializer()
//    {
//        // Arrange
//        var services = new ServiceCollection();
//        static void config(IKafkaConventionBuilder builder)
//        {
//            Debug.Write("config called");
//        }

//        // Act
//        services.AddMinimalKafka(config);
//        var serviceProvider = services.BuildServiceProvider();

//        // Assert
//        serviceProvider.GetService(typeof(JsonTextSerializer<string>)).Should().NotBeNull();
//    }

//    [Fact]
//    public void AddMinimalKafka_ShouldConfigureKafkaBuilder()
//    {
//        // Arrange
//        var services = new ServiceCollection();
                
//        static void config(IKafkaConfigBuilder builder) => 
//            builder.Should().BeOfType<KafkaConfigConventionBuilder>();

//        // Act
//        services.AddMinimalKafka(config);
//        var serviceProvider = services.BuildServiceProvider();
//        var kafkaBuilder = serviceProvider.GetService<IKafkaBuilder>();

//        // Assert
//        kafkaBuilder.Should().NotBeNull();
//    }

//    [Fact]
//    public void AddMinimalKafka_WithStreamStore_Should_Register()
//    {
//        // Arrange
//        var services = new ServiceCollection();

//        static void config(IKafkaConfigBuilder builder) =>
//            builder.WithInMemoryStore();

//        // Act
//        services.AddMinimalKafka(config);
//        var serviceProvider = services.BuildServiceProvider();
//        var kafkaBuilder = serviceProvider.GetService<IStreamStoreFactory>();

//        // Assert
//        kafkaBuilder.Should().NotBeNull();
//    }

//    [Fact]
//    public void AddMinimalKafka_Should_Set_Default_Config()
//    {
//        var services = new ServiceCollection();

//        static void config(IKafkaConfigBuilder builder) => 
//            builder.WithClientId("test");

//        // Act
//        services.AddMinimalKafka(config);
//        var serviceProvider = services.BuildServiceProvider();
//        var kafkaBuilder = serviceProvider.GetRequiredService<IKafkaBuilder>();


//        kafkaBuilder.ClientId.Should().Be("test");
//        kafkaBuilder.GroupId.Should().Be(AppDomain.CurrentDomain.FriendlyName);
       
//    }
//}