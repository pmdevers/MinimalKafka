using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;
using MinimalKafka.Serializers;

namespace MinimalKafka.Tests;

public class AddKafkaBuilderTests
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
        var kafkaBuilder = serviceProvider.GetService<IKafkaBuilder>();
        Assert.NotNull(kafkaBuilder);
    }

    [Fact]
    public void AddMinimalKafka_ShouldApplyCustomConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        // Act
        services.AddMinimalKafka(config =>
        {
            config.WithClientId("TestClient");
            config.WithGroupId("TestGroup");
        });
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var kafkaBuilder = serviceProvider.GetRequiredService<IKafkaBuilder>();
        Assert.Equal("TestClient", kafkaBuilder.MetaData.ProducerConfig().ClientId);
        Assert.Equal("TestGroup", kafkaBuilder.MetaData.ConsumerConfig().GroupId);
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
        var serializer = serviceProvider.GetService<IKafkaSerializer<string>>();
        Assert.NotNull(serializer);
    }

    [Fact]
    public void AddMinimalKafka_WithStore_ShouldRegisterCustomStoreFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        // Act
        services.AddMinimalKafka(x => x.WithStoreFactory(c => new TestKafkaStoreFactory(c)));
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var storeFactory = serviceProvider.GetService<IKafkaStoreFactory>();
        Assert.NotNull(storeFactory);
        Assert.IsType<TestKafkaStoreFactory>(storeFactory);
    }

    [Fact]
    public void AddMinimalKafka_ShouldRegisterInMemoryStoreFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        // Act
        services.AddMinimalKafka();
        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var storeFactory = serviceProvider.GetService<IKafkaStoreFactory>();
        Assert.NotNull(storeFactory);
        Assert.IsType<KafkaInMemoryStoreFactory>(storeFactory);
    }
}


public class TestKafkaStoreFactory(IServiceProvider serviceProvider) : IKafkaStoreFactory
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        // Nothing to dispose in this test implementation
    }

    public IKafkaStore GetStore(string topicName)
    {
        return new TestKafkaStore(serviceProvider);
    }
}

public class TestKafkaStore(IServiceProvider serviceProvider) : IKafkaStore
{
    public IServiceProvider ServiceProvider => serviceProvider;

    public ValueTask<byte[]> AddOrUpdate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        return ValueTask.FromResult(value.ToArray());
    }
    public ValueTask<byte[]?> FindByKeyAsync(ReadOnlySpan<byte> key)
    {
        return ValueTask.FromResult<byte[]?>(null);
    }
    public async IAsyncEnumerable<byte[]> GetItems()
    {
        yield break;
    }
}