using Confluent.Kafka;
using MinimalKafka.Metadata;

namespace MinimalKafka.Tests;
public class ConsumerBuilderTest
{
    [Fact]
    public void CreateFromMetaData()
    {
        List<object> metadata = [
          new GroupIdMetadata("test"),
          new BootstrapServersMetadata("nas.home.lab:9092"),
          new KeyDeserializerMetaData((s, t) => Substitute.For<IDeserializer<string>>()),
          new ValueDeserializerMetaData((s, t) => Substitute.For<IDeserializer<int>>())
        ];

        var builder = new MetadataConsumerBuilder<string, int>(metadata, EmptyServiceProvider.Instance)
            .Build();

        builder.Should().NotBeNull();
    }
}
