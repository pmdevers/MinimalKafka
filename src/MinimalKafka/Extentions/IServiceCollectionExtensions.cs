using MinimalKafka;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMinimalKafka(this IServiceCollection services)
    {
        services.AddSingleton<ITopicConsumerBuilder, DefaultTopicConsumerBuilder>();
        return services;
    }
}
