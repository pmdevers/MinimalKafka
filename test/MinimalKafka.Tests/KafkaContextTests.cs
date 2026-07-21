using Microsoft.Extensions.DependencyInjection;
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

        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var context = KafkaContext.Create(KafkaConsumerKey.Random("topic"), [],
            new()
            {
                Key = key,
                Value = value
            },
            serviceProvider);

        // Assert
        context.Should().BeAssignableTo<KafkaContext>();
        context.Key.SequenceEqual(key).Should().BeTrue();
        context.Value.SequenceEqual(value).Should().BeTrue();

        // Cleanup
        context.Dispose();
        serviceProvider.Dispose();
    }
}
