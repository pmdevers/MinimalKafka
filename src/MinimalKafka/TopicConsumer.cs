using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Serializers;

namespace MinimalKafka;

internal class TopicConsumer
{
    private readonly Topic _topic;
    private readonly IServiceProvider _serviceProvider;

    public TopicConsumer(Topic topic, IServiceProvider serviceProvider)
    {
        _topic = topic;
        _serviceProvider = serviceProvider;
    }

    public void StartAsync(CancellationToken cancellation)
    {
        var consumer = BuildConsumer();

        consumer.Subscribe(_topic.TopicName);

        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var result = consumer.Consume(cancellation);

                if (result is not null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = KafkaContext.Create(result, scope.ServiceProvider);

                    _topic.TopicHandler.Invoke(context);
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            consumer.Close();
        }
    }

    private IConsumer BuildConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "nas.home.lab:9092",
            GroupId = "Bla", // Use type name for unique group ID
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        return new ConsumerBuilder(config)
            .Build();
    }
}
