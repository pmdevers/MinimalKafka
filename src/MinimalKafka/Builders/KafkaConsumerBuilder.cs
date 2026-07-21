using Microsoft.Extensions.DependencyInjection;
using MinimalKafka.Internals;
using System.Diagnostics.Contracts;

namespace MinimalKafka.Builders;

internal sealed class KafkaConsumerBuilder(IServiceProvider serviceProvider) : IKafkaConsumerBuilder
{
    private KafkaConsumerKey? _key;
    private IReadOnlyList<object> _metadata = [];

    public IKafkaConsumerBuilder WithKey(KafkaConsumerKey key)
    {
        _key = key;
        return this;
    }

    public IKafkaConsumerBuilder WithMetadata(IReadOnlyList<object> metadata)
    {
        _metadata = metadata;
        return this;
    }

    [Pure]
    public IKafkaConsumer Build()
    {
        if (_key == null)
        {
            throw new InvalidOperationException("KafkaConsumerKey must be provided before building the consumer.");
        }

        return ActivatorUtilities.CreateInstance<KafkaConsumer>(serviceProvider, _key, _metadata);
    }
}
