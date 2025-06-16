using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Builders.Internals;

internal class AddKafkaBuilder(IServiceCollection services, ICollection<Action<IKafkaBuilder>> conventions)
    : KafkaConventionBuilder(conventions, []), IAddKafkaBuilder
{
    public IServiceCollection Services { get; } = services;
}
