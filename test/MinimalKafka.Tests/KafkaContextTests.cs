using Confluent.Kafka;

namespace MinimalKafka.Tests;

public class KafkaContextTests
{
    [Fact]
    public void KafkaContext_Empty_ShouldReturnEmptyContext()
    {
        // Arrange & Act
        var emptyContext = KafkaContext.Empty;

        // Assert
        emptyContext.Should().BeOfType<EmptyKafkaContext>();
        emptyContext.Key.Should().BeNull();
        emptyContext.Value.Should().BeNull();
        emptyContext.Headers.Should().BeEmpty();
        emptyContext.RequestServices.Should().Be(EmptyServiceProvider.Instance);
    }

    [Fact]
    public void KafkaContext_Create_ShouldReturnEmptyForInvalidResult()
    {
        // Arrange
        var invalidResult = new object();
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var context = KafkaContext.Create(invalidResult, serviceProvider, []);

        // Assert
        context.Should().BeSameAs(KafkaContext.Empty);
    }

    [Fact]
    public void KafkaContext_Create_ShouldReturnKafkaContextForValidResult()
    {
        // Arrange
        var key = "testKey";
        var value = "testValue";
        var headers = new Headers();
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = headers
            }
        };

        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var context = KafkaContext.Create(consumeResult, serviceProvider, []);

        // Assert
        context.Should().BeOfType<KafkaContext<string, string>>();
        context.Key.Should().Be(key);
        context.Value.Should().Be(value);
        context.Headers.Should().BeSameAs(headers);
        context.RequestServices.Should().BeSameAs(serviceProvider);
    }

    [Fact]
    public void EmptyKafkaContext_ShouldHaveNullKeyAndValue()
    {
        // Arrange
        var emptyContext = KafkaContext.Empty;

        // Act & Assert
        emptyContext.Key.Should().BeNull();
        emptyContext.Value.Should().BeNull();
    }

    [Fact]
    public void EmptyKafkaContext_ShouldHaveEmptyHeaders()
    {
        // Arrange
        var emptyContext = KafkaContext.Empty;

        // Act & Assert
        emptyContext.Headers.Should().BeEmpty();
    }

    [Fact]
    public void EmptyKafkaContext_ShouldUseEmptyServiceProvider()
    {
        // Arrange
        var emptyContext = KafkaContext.Empty;

        // Act & Assert
        emptyContext.RequestServices.Should().BeSameAs(EmptyServiceProvider.Instance);
    }

    [Fact]
    public void KafkaContext_Generic_ShouldReturnCorrectKeyAndValue()
    {
        // Arrange
        var key = "testKey";
        var value = "testValue";
        var headers = new Headers();
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = headers
            }
        };

        var serviceProvider = Substitute.For<IServiceProvider>();
        var context = new KafkaContext<string, string>(consumeResult, serviceProvider, []);

        // Act & Assert
        context.Key.Should().Be(key);
        context.Value.Should().Be(value);
        context.Headers.Should().BeSameAs(headers);
        context.RequestServices.Should().BeSameAs(serviceProvider);
    }
}
