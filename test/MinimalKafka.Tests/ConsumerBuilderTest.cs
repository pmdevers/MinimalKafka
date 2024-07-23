using Confluent.Kafka;
using MinimalKafka.Metadata;

namespace MinimalKafka.Tests;
public class ConsumerBuilderTest
{
    [Fact]
    public void CreateConsumerFromMetaData()
    {
        List<object> metadata = [
          new GroupIdMetadata("test"),
          new BootstrapServersMetadata("nas.home.lab:9092"),
          new KeyDeserializerMetadata((s) => Substitute.For<IDeserializer<string>>()),
          new ValueDeserializerMetadata((s) => Substitute.For<IDeserializer<int>>())
        ];

        var builder = new KafkaConsumerBuilder<string, int>(metadata, EmptyServiceProvider.Instance)
            .Build();

        builder.Should().NotBeNull();
    }


    [Fact]
    public void CreateProducerFromMetaData()
    {
        List<object> metadata = [
          new GroupIdMetadata("test"),
          new BootstrapServersMetadata("nas.home.lab:9092"),
          new KeySerializerMetadata((s) => Substitute.For<ISerializer<string>>()),
          new ValueSerializerMetadata((s) => Substitute.For<ISerializer<int>>())
        ];

        var builder = new KafkaProducerBuilder<string, int>(metadata, EmptyServiceProvider.Instance)
            .Build();

        builder.Should().NotBeNull();
    }
}
