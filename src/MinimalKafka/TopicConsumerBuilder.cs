using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinimalKafka.Attributes;

namespace MinimalKafka;

internal sealed class TopicConsumerBuilder : ITopicConsumerBuilder
{
    private readonly KafkaOptions _options;
    private static Delegate Empty = ([FromKey]string _, [FromValue]string _) => { };

    public string Topic { get; private set; } = string.Empty;

    public Delegate Handler { get; private set; } = Empty;

    public TopicConsumerBuilder(KafkaOptions options)
    {
        _options = options;
    }

    public ITopicConsumerBuilder MapTopic(string topic, Delegate handler)
    {
        Topic = topic;
        Handler = handler;
        return this;
    }

    public IConsumer<string, string> Build()
    {
        var config = new ClientConfig
        {
            BootstrapServers = _options.BoostrapServers,
        };

        var consumerConfig = new ConsumerConfig(config)
        {
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        var consumerBuilder = new ConsumerBuilder<string, string>(consumerConfig);

        return consumerBuilder.Build();
    }
}

public interface ITopicConsumerBuilder
{
    ITopicConsumerBuilder MapTopic(string topic, Delegate handler);
}

public static class MinmalKafkaExtensions
{
    public static ITopicConsumerBuilder MapTopic(this WebApplication app, string topic, Delegate handler)
    {
        var kafka = app.Services.GetRequiredService<TopicConsumerBuilder>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>();
        var builder = kafka.MapTopic(topic, handler);
        var s = new ConsumerService(logger.CreateLogger<ConsumerService>(), kafka);
        s.StartConsuming(CancellationToken.None);
        return builder;
    }
}
