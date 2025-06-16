using Microsoft.Extensions.DependencyInjection;

namespace MinimalKafka.Builders;

public interface IAddKafkaBuilder : IKafkaConventionBuilder
{
    IServiceCollection Services { get; }
}
