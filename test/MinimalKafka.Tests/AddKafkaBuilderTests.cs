using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MinimalKafka.Runtime;
using MinimalKafka.Serialization;

namespace MinimalKafka.Tests;

public class MinimalKafkaExtensionsTests
{
    [Fact]
    public void AddMinimalKafka_ShouldRegisterKafkaBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        // Act
        services.AddMinimalKafka();
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var kafkaBuilder = serviceProvider.GetService<TopicRegistry>();
        Assert.NotNull(kafkaBuilder);
    }

    [Fact]
    public void AddMinimalKafka_ShouldApplyCustomConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddOptions();
        // Act
        services.AddMinimalKafka(config =>
        {
            config.WithClientId("TestClient");
            config.WithGroupId("TestGroup");
        });
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var kafkaBuilder = serviceProvider.GetRequiredService<IOptions<KafkaConsumerOptions>>();
        Assert.Equal("TestClient", kafkaBuilder.Value.CreateProducerConfig().ClientId);
        Assert.Equal("TestGroup", kafkaBuilder.Value.CreateConsumerConfig().GroupId);
    }

    [Fact]
    public void AddMinimalKafka_ShouldRegisterJsonSerializers()
    {
        // Arrange
        var services = new ServiceCollection();
        // Act
        services.AddMinimalKafka();
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var serializer = serviceProvider.GetService<IKafkaSerializer>();
        Assert.NotNull(serializer);
    }

    //[Fact]
    //public void AddMinimalKafka_WithStore_ShouldRegisterCustomStoreFactory()
    //{
    //    // Arrange
    //    var services = new ServiceCollection();
    //    // Act
    //    services.AddMinimalKafka(x => x.WithStoreFactory(c => new TestKafkaStoreFactory(c)));
    //    // Assert
    //    var serviceProvider = services.BuildServiceProvider();
    //    var storeFactory = serviceProvider.GetService<IKafkaStoreFactory>();
    //    Assert.NotNull(storeFactory);
    //    Assert.IsType<TestKafkaStoreFactory>(storeFactory);
    //}

    //[Fact]
    //public void AddMinimalKafka_ShouldRegisterInMemoryStoreFactory()
    //{
    //    // Arrange
    //    var services = new ServiceCollection();
    //    // Act
    //    services.AddMinimalKafka();
    //    // Assert
    //    var serviceProvider = services.BuildServiceProvider();
    //    var storeFactory = serviceProvider.GetService<IKafkaStoreFactory>();
    //    Assert.NotNull(storeFactory);
    //    Assert.IsType<KafkaInMemoryStoreFactory>(storeFactory);
    //}
}


//public class TestKafkaStoreFactory(IServiceProvider serviceProvider) : IKafkaStoreFactory
//{
//    public void Dispose()
//    {
//        GC.SuppressFinalize(this);
//        // Nothing to dispose in this test implementation
//    }

//    public IKafkaStore GetStore(string topicName)
//    {
//        return new TestKafkaStore(serviceProvider);
//    }
//}

//public class TestKafkaStore(IServiceProvider serviceProvider) : IKafkaStore
//{
//    public IServiceProvider ServiceProvider => serviceProvider;

//    public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
//    {
//        return ValueTask.FromResult(value.ToArray());
//    }
//    public ValueTask<byte[]?> FindByKeyAsync(ReadOnlySpan<byte> key)
//    {
//        return ValueTask.FromResult<byte[]?>(null);
//    }
//    public async IAsyncEnumerable<byte[]> GetItems()
//    {
//        yield break;
//    }
//}