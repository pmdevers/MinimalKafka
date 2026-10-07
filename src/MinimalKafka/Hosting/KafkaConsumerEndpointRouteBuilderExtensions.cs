using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Runtime;

namespace MinimalKafka.Hosting;

public static class KafkaConsumerEndpointRouteBuilderExtensions
{
    public static TopicBuilder MapTopic(
        this IEndpointRouteBuilder endpoints,
        string topic,
        Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var registration = endpoints.ServiceProvider.GetRequiredService<TopicRegistry>().Add(topic, handler);
        return new TopicBuilder(registration);
    }
}
