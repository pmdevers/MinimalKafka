using Confluent.Kafka;
using MinimalKafka.Metadata;
using MinimalKafka.Builders;

namespace MinimalKafka.Tests;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.

public class KafkaDelegateFactoryTests
{
    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenHandlerIsNull()
    {
        // Arrange
        Delegate handler = null;

        // Act
        Action act = () => KafkaDelegateFactory.Create(handler);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldReturnKafkaDelegateResult()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var kafkaBuilder = Substitute.For<IKafkaBuilder>();

        kafkaBuilder.MetaData.Returns([new BootstrapServersMetadata("localhost:9092")]);

        var options = new KafkaDelegateFactoryOptions
        {
            ServiceProvider = serviceProvider,
            KafkaBuilder = kafkaBuilder
        };

        Func<KafkaContext, Task> handler = kafkaContext => Task.CompletedTask;

        // Act
        var result = KafkaDelegateFactory.Create(handler, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<KafkaDelegateResult>();
        result.Delegate.Should().BeOfType<KafkaDelegate>();
        result.Metadata.Should().HaveCount(1);
        result.KeyType.Should().Be(typeof(Ignore));
        result.ValueType.Should().Be(typeof(Ignore));
    }

    [Fact]
    public void Create_ShouldReturnCorrectFinalKafkaDelegate()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var kafkaBuilder = Substitute.For<IKafkaBuilder>();
        var options = new KafkaDelegateFactoryOptions
        {
            ServiceProvider = serviceProvider,
            KafkaBuilder = kafkaBuilder
        };

        Func<KafkaContext, Task> handler = kafkaContext => Task.CompletedTask;

        // Act
        var result = KafkaDelegateFactory.Create(handler, options);

        // Assert
        result.Delegate.Should().NotBeNull();
    }

    [Fact]
    public async Task FinalKafkaDelegate_ShouldInvokeHandler()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var kafkaBuilder = Substitute.For<IKafkaBuilder>();
        var options = new KafkaDelegateFactoryOptions
        {
            ServiceProvider = serviceProvider,
            KafkaBuilder = kafkaBuilder
        };

        var wasCalled = false;
        Func<KafkaContext, Task> handler = kafkaContext =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        };

        // Act
        var result = KafkaDelegateFactory.Create(handler, options);
        await result.Delegate.Invoke(KafkaContext.Empty);

        // Assert
        wasCalled.Should().BeTrue();
    }

    [Fact]
    public void Async_Handler_with_no_parameters_Should_not_Throw()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var kafkaBuilder = Substitute.For<IKafkaBuilder>();
        var options = new KafkaDelegateFactoryOptions
        {
            ServiceProvider = serviceProvider,
            KafkaBuilder = kafkaBuilder
        };
        Func<Task> handler = () => Task.CompletedTask;
        // Act
        var result = KafkaDelegateFactory.Create(handler, options);
        // Assert
        result.KeyType.Should().Be(typeof(Ignore));
        result.ValueType.Should().Be(typeof(Ignore));
    }

    [Fact]
    public void Void_handler_should_not_throw()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var kafkaBuilder = Substitute.For<IKafkaBuilder>();
        var options = new KafkaDelegateFactoryOptions
        {
            ServiceProvider = serviceProvider,
            KafkaBuilder = kafkaBuilder
        };
        Action handler = () => { };
        // Act
        var result = KafkaDelegateFactory.Create(handler, options);
        // Assert
        result.KeyType.Should().Be(typeof(Ignore));
        result.ValueType.Should().Be(typeof(Ignore));
    }
}
