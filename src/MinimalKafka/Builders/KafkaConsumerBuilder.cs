using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalKafka.Internals;
using MinimalKafka.Metadata;
using MinimalKafka.Metadata.Internals;

namespace MinimalKafka.Builders;

internal class KafkaConsumerBuilder(KafkaConsumerKey key, IKafkaBuilder builder) : IKafkaConsumerBuilder
{
    private List<KafkaDelegate> Delegates { get; } = [];

    public IKafkaConsumer Build()
    {
        var consumer = CreateConsumer();
        var producer = builder.ServiceProvider.GetRequiredService<IKafkaProducer>();
        var logger = builder.ServiceProvider.GetRequiredService<ILogger<KafkaConsumer>>();
        var formatter = builder.ServiceProvider.GetRequiredService<KafkaTopicFormatter>();

        return new KafkaConsumer(
            key,
            true,
            consumer,
            producer,
            [.. Delegates],
            formatter,
            [.. builder.MetaData],
            builder.ServiceProvider,
            logger
        );
    }

    private IConsumer<byte[], byte[]> CreateConsumer()
    {
        var config = builder.MetaData.OfType<IConfigMetadata>().First();
        var handlers = builder.MetaData.OfType<IConsumerHandlerMetadata>().FirstOrDefault() ??
            new ConsumerHandlerMetadata();

        return new ConsumerBuilder<byte[], byte[]>(config.ConsumerConfig.AsEnumerable())
            .SetKeyDeserializer(Deserializers.ByteArray)
            .SetValueDeserializer(Deserializers.ByteArray)
            .SetStatisticsHandler(handlers?.StatisticsHandler)
            .SetErrorHandler(handlers?.ErrorHandler)
            .SetLogHandler(handlers?.LogHandler)
            .SetPartitionsAssignedHandler(handlers?.PartitionsAssignedHandler)
            .SetPartitionsLostHandler(handlers?.PartitionsLostHandler)
            .SetPartitionsRevokedHandler((c, partitions) =>
            {
                Console.WriteLine($"Partitions revoked: [{string.Join(", ", partitions)}]");
                try
                {
                    handlers?.PartitionsRevokedHandler?.Invoke(c, partitions);

                    //c.Commit();  // commit offsets before losing partitions
                    Console.WriteLine("Offsets committed on revoke.");
                }
                catch (KafkaException e) when (e.Error.Code == ErrorCode.Local_State)
                {
                    Console.WriteLine($"Skip commit on revoke: {e.Error.Reason}");
                }
            })
            .Build();
    }

    internal void AddDelegate(KafkaDelegate del)
    {
       Delegates.Add( del );
    }
}
