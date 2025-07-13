using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Internals;
using MinimalKafka.Metadata;

namespace MinimalKafka.Builders;

internal class KafkaConsumerBuilder(KafkaConsumerKey key, IKafkaBuilder builder) : IKafkaConsumerBuilder
{
    private List<KafkaDelegate> Delegates { get; } = [];

    public IKafkaConsumer Build()
    {
        var consumer = CreateConsumer();
        var producer = CreateProducer();
        var store = builder.ServiceProvider.GetRequiredService<IKafkaConsumerStore>();
        var logger = builder.ServiceProvider.GetRequiredService<ILogger<KafkaConsumer>>();

        return new KafkaConsumer(
            key,
            true,
            consumer,
            producer,
            store,
            [.. Delegates],
            builder.ServiceProvider,
            logger
        );
    }

    private IConsumer<byte[], byte[]> CreateConsumer()
    {
        var config = builder.MetaData.OfType<IConfigMetadata>().First();

        return new ConsumerBuilder<byte[], byte[]>(config.ConsumerConfig.AsEnumerable())
            .SetKeyDeserializer(Deserializers.ByteArray)
            .SetValueDeserializer(Deserializers.ByteArray)
            .Build();
    }

    private KafkaContextProducer CreateProducer()
    {
        var config = builder.MetaData.OfType<IConfigMetadata>().First();

        var producer = new ProducerBuilder<byte[], byte[]>(config.ProducerConfig.AsEnumerable())
            .SetKeySerializer(Confluent.Kafka.Serializers.ByteArray)
            .SetValueSerializer(Confluent.Kafka.Serializers.ByteArray)
            .Build();
        return new KafkaContextProducer(producer);
    }

    internal void AddDelegate(KafkaDelegate del)
    {
       Delegates.Add( del );
    }
}
