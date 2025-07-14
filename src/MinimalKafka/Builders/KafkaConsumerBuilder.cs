using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;

namespace MinimalKafka.Builders;

internal class KafkaConsumerBuilder(KafkaConsumerKey key, IKafkaBuilder builder) : IKafkaConsumerBuilder
{
    private List<KafkaDelegate> Delegates { get; } = [];

    public IKafkaConsumer Build()
    {
        var conf = KafkaConsumerConfig.Create(key, Delegates, builder.MetaData);
        
        return ActivatorUtilities.CreateInstance<KafkaConsumer>(
            builder.ServiceProvider,
            conf
        );
    }

    

    internal void AddDelegate(KafkaDelegate del)
    {
       Delegates.Add( del );
    }
}
