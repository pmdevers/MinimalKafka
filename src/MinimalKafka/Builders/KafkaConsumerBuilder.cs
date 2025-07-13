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
        var producer = builder.ServiceProvider.GetRequiredService<IKafkaProducer>();
        var logger = builder.ServiceProvider.GetRequiredService<ILogger<KafkaConsumer>>();

        return new KafkaConsumer(
            key,
            true,
            consumer,
            producer,
            [.. Delegates],
            [.. builder.MetaData],
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

    internal void AddDelegate(KafkaDelegate del)
    {
       Delegates.Add( del );
    }
}
