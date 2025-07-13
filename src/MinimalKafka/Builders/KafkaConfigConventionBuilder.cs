using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Builders;

internal class KafkaConfigConventionBuilder(IServiceCollection services, ICollection<Action<IKafkaBuilder>> conventions)
    : KafkaConventionBuilder(conventions, []),
    IKafkaConfigBuilder
{
    public IServiceCollection Services { get; } = services;
}
