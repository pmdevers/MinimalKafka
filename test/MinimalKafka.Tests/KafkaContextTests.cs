using Confluent.Kafka;
using MinimalKafka.Internals;
using System.Text;
using System.Text.Unicode;

namespace MinimalKafka.Tests;

public class KafkaContextTests
{
    [Fact]
    public void KafkaContext_Create_ShouldReturnKafkaContextForValidResult()
    {
        // Arrange
        var key = Encoding.UTF8.GetBytes("testKey");
        var value = Encoding.UTF8.GetBytes("testValue");
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

        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var context = KafkaContext.Create(KafkaConsumerKey.Random("topic"), consumeResult.Message, serviceProvider);

        // Assert
        context.Should().BeOfType<KafkaContext>();
        context.Key.SequenceEqual(key).Should().BeTrue();
        context.Value.SequenceEqual(value).Should().BeTrue();
        context.RequestServices.Should().BeSameAs(serviceProvider);
    }

    [Fact]
    public void KafkaContext_Generic_ShouldReturnCorrectKeyAndValue()
    {
        // Arrange
        var key = Encoding.UTF8.GetBytes("testKey");
        var value = Encoding.UTF8.GetBytes("testValue");
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

        var serviceProvider = Substitute.For<IServiceProvider>();
        var context = KafkaContext.Create(KafkaConsumerKey.Random("topic"), consumeResult.Message, serviceProvider);

        // Act & Assert
        context.Key.SequenceEqual(key).Should().BeTrue();
        context.Value.SequenceEqual(value).Should().BeTrue();
        context.Headers.Should().BeSameAs(headers.ToDictionary(x => x.Key, y => y.GetValueBytes()));
        context.RequestServices.Should().BeSameAs(serviceProvider);
    }
}
