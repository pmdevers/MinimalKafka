using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka;

internal sealed class TopicConsumerBuilder : ITopicConsumerBuilder
{
    public IConsumerService ConsumerService { get; set; }

    public TopicConsumerBuilder(IConsumerService consumerService)
    {
        ConsumerService = consumerService;
    }

    public ITopicConsumerBuilder MapTopic(string topic, Delegate handler)
    {
        ConsumerService.AddTopic(topic, handler);
        return this;
    }
}

internal interface IConsumerService
{
    void AddTopic(string topic, Delegate handler);
}

public interface ITopicConsumerBuilder
{
    ITopicConsumerBuilder MapTopic(string topic, Delegate handler);
}

public static class MinmalKafkaExtensions
{
    public static ITopicConsumerBuilder MapTopic(this WebApplication app, string topic, Delegate handler)
    {
        var consumerService = app.Services.GetRequiredService<IConsumerService>();
        return new TopicConsumerBuilder(consumerService).MapTopic(topic, handler);
    }

    public static IServiceCollection AddMinimalKafka(this IServiceCollection services)
        => services.AddSingleton<IConsumerService, ConsumerService>();
}
