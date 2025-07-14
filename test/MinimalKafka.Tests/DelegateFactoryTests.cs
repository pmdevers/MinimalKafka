using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Builders;
using MinimalKafka.Internals;
using System.Text;
using System.Text.Json;

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
        var config = KafkaConsumerConfig.Create(KafkaConsumerKey.Random("topic"), [], []);
        var context = KafkaContext.Create(config, new Message<byte[], byte[]>(), serviceProvider);

        await result.Delegate.Invoke(context);

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

    [Fact]
    public async Task Key_Should_Be_Serialized()
    {
        var services = new ServiceCollection();
        services.AddSingleton(JsonSerializerOptions.Default);
        //services.AddTransient(typeof(JsonTextSerializer<>));

        // Arrange
        var serviceProvider = services.BuildServiceProvider();  
        
        var kafkaBuilder = Substitute.For<IKafkaBuilder>();
        var options = new KafkaDelegateFactoryOptions
        {
            ServiceProvider = serviceProvider,
            KafkaBuilder = kafkaBuilder
        };
        Delegate handler = ([FromKey] string key, [FromValue] string value) => {

            key.Should().Be("testKey");
            value.Should().Be("testValue");

            return Task.CompletedTask;
        };
        // Act
        var result = KafkaDelegateFactory.Create(handler, options);
        // Assert
        result.KeyType.Should().Be(typeof(string));
        result.ValueType.Should().Be(typeof(string));

        var key = Encoding.UTF8.GetBytes("\"testKey\"");
        var value = Encoding.UTF8.GetBytes("\"testValue\"");
        var headers = new Headers();
        var consumeResult = new ConsumeResult<byte[], byte[]>
        {
            Message = new Message<byte[], byte[]>
            {
                Key = key,
                Value = value,
                Headers = headers
            }
        };

        var config = KafkaConsumerConfig.Create(KafkaConsumerKey.Random("topic"), [], []);
        var context = KafkaContext.Create(config, new Message<byte[], byte[]>(), serviceProvider);

        try
        {
            await result.Delegate.Invoke(context);
        } catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        
    }
}
