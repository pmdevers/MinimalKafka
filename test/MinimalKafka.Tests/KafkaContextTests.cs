using Confluent.Kafka;
using MinimalKafka.Internals;
using System.Text;

namespace MinimalKafka.Tests;

public class KafkaContextTests
{
    [Fact]
    public void KafkaContext_Create_ShouldReturnKafkaContextForValidResult()
    {
        // Arrange
        var key = Encoding.UTF8.GetBytes("testKey");
        var value = Encoding.UTF8.GetBytes("testValue");
        
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var context = KafkaContext.Create("topic", [],
            new()
            {
                Key = key,
                Value = value
            }, 
            serviceProvider);

        // Assert
        context.Should().BeOfType<KafkaContext>();
        context.Key.SequenceEqual(key).Should().BeTrue();
        context.Value.SequenceEqual(value).Should().BeTrue();
        context.RequestServices.Should().BeSameAs(serviceProvider);
    }
}
