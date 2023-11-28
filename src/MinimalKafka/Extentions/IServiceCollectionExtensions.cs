using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalKafka;
using System.Collections.ObjectModel;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMinimalKafka(this IServiceCollection services)
    {
        services.AddSingleton<ITopicConsumerBuilder, DefaultTopicConsumerBuilder>();

        services.AddHostedService<TopicConsumerService>();

        return services;
    }
}
